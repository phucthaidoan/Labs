namespace FCMDemo.API.Data.Entities;

/// <summary>
/// Status of a scheduled notification
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Notification is scheduled but not yet sent
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Notification has been sent successfully
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Notification failed to send
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Notification was cancelled before sending
    /// </summary>
    Cancelled = 3
}
