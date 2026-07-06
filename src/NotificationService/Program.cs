using NotificationService.Data;
using NotificationService.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<NotificationDbSettings>(builder.Configuration.GetSection("NotificationDatabase"));
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "NotificationService" }));

app.Run();
