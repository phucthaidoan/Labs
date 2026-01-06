namespace FCMDemo.API.Models;

/// <summary>
/// Response model for notification send operations
/// </summary>
public class NotificationResponse
{
    /// <summary>
    /// Indicates if the notification was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Firebase message ID (if successful)
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Target device token
    /// </summary>
    public string? DeviceToken { get; set; }
}
