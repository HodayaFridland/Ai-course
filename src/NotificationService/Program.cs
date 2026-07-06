using NotificationService.Consumers;
using NotificationService.Data;
using NotificationService.Messaging;
using NotificationService.Repositories;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Structured logging to console + Seq (Phase 5) ---
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotificationService")
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<NotificationDbSettings>(builder.Configuration.GetSection("NotificationDatabase"));
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();

// Phase 4: listen for the order's final state and notify the customer.
builder.Services.AddSingleton<RabbitMqEventBus>();
builder.Services.AddHostedService<NotificationSagaConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

app.Run();
