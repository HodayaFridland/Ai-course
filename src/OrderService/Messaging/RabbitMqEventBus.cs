using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Messaging;

/// <summary>
/// The one small RabbitMQ wrapper every service in the saga uses (an identical copy lives in
/// InventoryService and NotificationService — services don't share code, so each owns its copy).
///
/// Topology: a single durable FANOUT exchange "orders.events". Each service declares its OWN
/// durable queue bound to that exchange, so every published event reaches every service; each
/// service then reacts only to the event types it cares about.
///
/// The event name and the correlation id travel in the AMQP message properties (Type / CorrelationId).
/// That is how the correlation id survives the trip THROUGH the broker, not just over HTTP.
///
/// Two channels on purpose: RabbitMQ's IModel is NOT thread-safe, and OrderService both publishes
/// (from web requests) and consumes (from the background saga) at the same time. So we keep a
/// dedicated publish channel (guarded by a lock) separate from the consume channel.
/// </summary>
public class RabbitMqEventBus : IDisposable
{
    private const string ExchangeName = "orders.events";

    private readonly IConnection _connection;
    private readonly IModel _consumeChannel;
    private readonly IModel _publishChannel;
    private readonly object _publishLock = new();
    private readonly string _queueName;
    private readonly ILogger<RabbitMqEventBus> _logger;

    public RabbitMqEventBus(IConfiguration config, ILogger<RabbitMqEventBus> logger)
    {
        _logger = logger;
        var host = config["MessageBroker:Host"] ?? "localhost";
        _queueName = config["MessageBroker:Queue"] ?? "order.saga";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = 5672,
            DispatchConsumersAsync = true,     // lets us use async message handlers
            AutomaticRecoveryEnabled = true
        };

        _connection = ConnectWithRetry(factory);
        _consumeChannel = _connection.CreateModel();
        _publishChannel = _connection.CreateModel();

        _consumeChannel.ExchangeDeclare(ExchangeName, ExchangeType.Fanout, durable: true);
        _consumeChannel.QueueDeclare(_queueName, durable: true, exclusive: false, autoDelete: false);
        _consumeChannel.QueueBind(_queueName, ExchangeName, routingKey: string.Empty);

        _logger.LogInformation("RabbitMQ connected at {Host}; queue '{Queue}' bound to exchange '{Exchange}'",
            host, _queueName, ExchangeName);
    }

    // RabbitMQ may still be booting when we start — retry a few times before giving up.
    private IConnection ConnectWithRetry(ConnectionFactory factory, int maxAttempts = 12)
    {
        for (var attempt = 1; ; attempt++)
        {
            try { return factory.CreateConnection(); }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning("RabbitMQ not ready (attempt {Attempt}/{Max}): {Message}. Retrying in 3s...",
                    attempt, maxAttempts, ex.Message);
                Thread.Sleep(3000);
            }
        }
    }

    /// <summary>Publish an event to the whole system. eventType + correlationId ride in the message properties.</summary>
    public void Publish(string eventType, object payload, string correlationId)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload);
        lock (_publishLock)
        {
            var props = _publishChannel.CreateBasicProperties();
            props.Type = eventType;
            props.CorrelationId = correlationId;
            props.ContentType = "application/json";
            props.DeliveryMode = 2; // persistent
            _publishChannel.BasicPublish(ExchangeName, routingKey: string.Empty, basicProperties: props, body: body);
        }
        _logger.LogInformation("[{CorrelationId}] --> published {EventType}", correlationId, eventType);
    }

    /// <summary>Start receiving events. The handler gets (eventType, jsonBody, correlationId).</summary>
    public void StartConsuming(Func<string, string, string, Task> handler)
    {
        var consumer = new AsyncEventingBasicConsumer(_consumeChannel);
        consumer.Received += async (_, ea) =>
        {
            var eventType = ea.BasicProperties.Type ?? string.Empty;
            var correlationId = ea.BasicProperties.CorrelationId ?? string.Empty;
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                await handler(eventType, json, correlationId);
                _consumeChannel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[{CorrelationId}] handler failed for {EventType}", correlationId, eventType);
                // Don't requeue: avoids a poison message looping forever. A real system would dead-letter it.
                _consumeChannel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };
        _consumeChannel.BasicConsume(_queueName, autoAck: false, consumer);
    }

    public void Dispose()
    {
        if (_consumeChannel.IsOpen) _consumeChannel.Close();
        if (_publishChannel.IsOpen) _publishChannel.Close();
        if (_connection.IsOpen) _connection.Close();
    }
}
