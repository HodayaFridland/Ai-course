using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InventoryService.Messaging;

/// <summary>
/// Identical RabbitMQ wrapper to the one in OrderService (each service owns its copy).
/// One durable FANOUT exchange "orders.events"; this service owns the "inventory.saga" queue.
/// eventType + correlationId travel in the AMQP message properties so the correlation id
/// survives the trip through the broker.
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
        _queueName = config["MessageBroker:Queue"] ?? "inventory.saga";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = 5672,
            DispatchConsumersAsync = true,
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

    public void Publish(string eventType, object payload, string correlationId)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(payload);
        lock (_publishLock)
        {
            var props = _publishChannel.CreateBasicProperties();
            props.Type = eventType;
            props.CorrelationId = correlationId;
            props.ContentType = "application/json";
            props.DeliveryMode = 2;
            _publishChannel.BasicPublish(ExchangeName, routingKey: string.Empty, basicProperties: props, body: body);
        }
        _logger.LogInformation("[{CorrelationId}] --> published {EventType}", correlationId, eventType);
    }

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
