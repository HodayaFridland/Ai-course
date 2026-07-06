using Microsoft.Extensions.Options;
using MongoDB.Driver;
using NotificationService.Models;

namespace NotificationService.Data;

public class MongoContext
{
    public IMongoCollection<Notification> Notifications { get; }

    public MongoContext(IOptions<NotificationDbSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        var database = client.GetDatabase(settings.DatabaseName);
        Notifications = database.GetCollection<Notification>(settings.NotificationsCollectionName);
    }
}
