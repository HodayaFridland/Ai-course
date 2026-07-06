using ProductCatalogService.Data;
using ProductCatalogService.Repositories;

var builder = WebApplication.CreateBuilder(args);

// --- Services (dependency injection) ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind the "CatalogDatabase" section of appsettings.json into CatalogDbSettings.
builder.Services.Configure<CatalogDbSettings>(builder.Configuration.GetSection("CatalogDatabase"));

// MongoContext is a singleton (the Mongo driver manages its own connection pool).
builder.Services.AddSingleton<MongoContext>();

// Repositories can be singletons too here — they hold no per-request state.
builder.Services.AddSingleton<IProductRepository, ProductRepository>();
builder.Services.AddSingleton<ICategoryRepository, CategoryRepository>();

var app = builder.Build();

// --- Seed sample data on startup ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await CatalogSeeder.SeedAsync(context, logger);
}

// --- HTTP pipeline ---
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// Health endpoint — used by docker-compose healthchecks and the gateway.
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "ProductCatalogService" }));

app.Run();
