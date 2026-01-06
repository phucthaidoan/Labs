namespace FCMDemo.API.Models;

/// <summary>
/// Response model for notification log
/// </summary>
public class NotificationHistoryResponse
{
    /// <summary>
    /// Log entry ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Device token
    /// </summary>
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Custom data (if any)
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Whether the notification was sent successfully
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Firebase message ID (if successful)
    /// </summary>
    public string? FirebaseMessageId { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the notification was sent (UTC)
    /// </summary>
    public DateTime SentAt { get; set; }

    /// <summary>
    /// Platform (Web, iOS, Android)
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Scheduled notification ID (if from scheduled)
    /// </summary>
    public int? ScheduledNotificationId { get; set; }
}

/// <summary>
/// Response model for notification statistics
/// </summary>
public class NotificationStatsResponse
{
    /// <summary>
    /// Total notifications sent to this user
    /// </summary>
    public int TotalSent { get; set; }

    /// <summary>
    /// Number of successful notifications
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Number of failed notifications
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// Success rate percentage (0-100)
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// When the last notification was sent
    /// </summary>
    public DateTime? LastSentAt { get; set; }

    /// <summary>
    /// When the first notification was sent
    /// </summary>
    public DateTime? FirstSentAt { get; set; }
}
