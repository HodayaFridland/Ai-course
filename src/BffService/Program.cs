using BffService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Structured logging to console + Seq (Phase 5) ---
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "BffService")
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The BFF talks to TWO downstream services and stitches their data together.
builder.Services.AddHttpClient("orders", client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:OrderService"]!));
builder.Services.AddHttpClient("catalog", client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogService"]!));

builder.Services.AddScoped<IOrderDetailsService, OrderDetailsService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "BffService" }));

app.Run();
