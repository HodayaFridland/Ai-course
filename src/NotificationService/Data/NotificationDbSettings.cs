namespace NotificationService.Data;

public class NotificationDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string NotificationsCollectionName { get; set; } = "notifications";
}
