using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Models;

/// <summary>
/// Request model for scheduling a notification
/// </summary>
public class ScheduleNotificationRequest
{
    /// <summary>
    /// User ID to send the notification to
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional specific device token. If null, will send to user's active device
    /// </summary>
    [MaxLength(500)]
    public string? DeviceToken { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body/message
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional custom data payload
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// When the notification should be sent (UTC)
    /// </summary>
    [Required]
    public DateTime ScheduledFor { get; set; }
}

/// <summary>
/// Response model for scheduled notification
/// </summary>
public class ScheduledNotificationResponse
{
    /// <summary>
    /// Scheduled notification ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Notification title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// When the notification is scheduled for (UTC)
    /// </summary>
    public DateTime ScheduledFor { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the notification was sent (if sent)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    public string? ErrorMessage { get; set; }
}
