using MongoDB.Driver;
using NotificationService.Data;
using NotificationService.Models;

namespace NotificationService.Repositories;

public interface INotificationRepository
{
    Task<List<Notification>> GetAllAsync();
    Task<List<Notification>> GetByUserAsync(int userId);
    Task<Notification> CreateAsync(Notification notification);
}

public class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _notifications;

    public NotificationRepository(MongoContext context)
    {
        _notifications = context.Notifications;
    }

    public async Task<List<Notification>> GetAllAsync() =>
        await _notifications.Find(Builders<Notification>.Filter.Empty)
                            .SortByDescending(n => n.CreatedAt).ToListAsync();

    public async Task<List<Notification>> GetByUserAsync(int userId) =>
        await _notifications.Find(n => n.UserId == userId)
                            .SortByDescending(n => n.CreatedAt).ToListAsync();

    public async Task<Notification> CreateAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        await _notifications.InsertOneAsync(notification);
        return notification;
    }
}
