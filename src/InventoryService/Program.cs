using InventoryService.Data;
using InventoryService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core against SQL Server. EnableRetryOnFailure helps while SQL Server is still starting up.
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure()));

builder.Services.AddScoped<IStockService, StockService>();

var app = builder.Build();

// Create the database/tables if needed and seed starting stock.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    db.Database.EnsureCreated();   // simple for a demo DB (no migrations needed)
    InventorySeeder.Seed(db, logger);
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "InventoryService" }));

app.Run();
