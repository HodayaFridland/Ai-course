using Microsoft.EntityFrameworkCore;
using OrderService.Clients;
using OrderService.Data;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Database (relational, for ACID) ---
builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

// --- Typed HttpClients: base addresses come from the "Services" config section ---
// In docker-compose these point at the other containers (e.g. http://catalogservice:8080).
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Catalog"]!));

builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Inventory"]!));

builder.Services.AddHttpClient<INotificationClient, NotificationClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["Services:Notification"]!));

builder.Services.AddScoped<IOrderProcessingService, OrderProcessingService>();

var app = builder.Build();

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
