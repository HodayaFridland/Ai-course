using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NotificationService.Models;

/// <summary>
/// A notification we sent (or would send) to a customer, stored as a MongoDB document.
/// This is an append-only history log — a natural fit for the document model, and eventual
/// consistency (BASE) is fine here: nothing breaks if a notification appears a second late.
/// </summary>
public class Notification
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public int OrderId { get; set; }
    public int UserId { get; set; }

    // "Confirmed" or "Rejected".
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
