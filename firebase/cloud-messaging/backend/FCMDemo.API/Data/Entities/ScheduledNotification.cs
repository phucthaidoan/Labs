using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Data.Entities;

/// <summary>
/// Represents a notification scheduled for future delivery
/// </summary>
public class ScheduledNotification
{
    /// <summary>
    /// Unique identifier for the scheduled notification
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID to send the notification to
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Optional specific device token. If null, will send to all user's devices
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
    /// Optional custom data payload (JSON)
    /// </summary>
    public string? DataJson { get; set; }

    /// <summary>
    /// When the notification should be sent (UTC)
    /// </summary>
    [Required]
    public DateTime ScheduledFor { get; set; }

    /// <summary>
    /// Current status of the scheduled notification
    /// </summary>
    [Required]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    /// <summary>
    /// When the notification was created (UTC)
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the notification was actually sent (UTC)
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Error message if sending failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Link to the notification log after it's sent
    /// </summary>
    public int? NotificationLogId { get; set; }

    /// <summary>
    /// Navigation property to the notification log
    /// </summary>
    public NotificationLog? NotificationLog { get; set; }
}
