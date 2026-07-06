using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Structured logging to console + Seq (Phase 5) ---
builder.Host.UseSerilog((context, config) => config
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "GatewayService")
    .WriteTo.Console()
    .WriteTo.Seq(context.Configuration["Seq:ServerUrl"] ?? "http://seq:5341"));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Serve the demo web client (wwwroot/index.html) from the gateway itself.
// Same origin as the proxied API calls, so the browser needs no CORS.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "GatewayService" }));

// Every /api/** call is routed to the right microservice by YARP (see appsettings.json).
app.MapReverseProxy();

app.Run();
