using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Consumers;
using OrderService.Data;
using OrderService.Messaging;
using OrderService.Middleware;
using OrderService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Structured logging to console + Seq (Phase 5) ---
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "OrderService")
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Database (relational, for ACID) ---
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

// Order still reads the catalog to PRICE items (a query, kept synchronous).
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"]!));

// The RabbitMQ event bus is a singleton (one connection per service).
builder.Services.AddSingleton<RabbitMqEventBus>();
builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();

// Background worker that advances the saga when inventory answers.
builder.Services.AddHostedService<OrderSagaConsumer>();

var app = builder.Build();

// Tag every request/log line with a correlation id (also flows into the events we publish).
app.UseMiddleware<CorrelationIdMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OrderService" }));

app.Run();
