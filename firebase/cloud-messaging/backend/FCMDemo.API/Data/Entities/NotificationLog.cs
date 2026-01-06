using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Data.Entities;

/// <summary>
/// Audit log of all sent notifications
/// </summary>
public class NotificationLog
{
    /// <summary>
    /// Unique identifier for the log entry
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// User ID who received the notification
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Device token the notification was sent to
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string DeviceToken { get; set; } = string.Empty;

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
    /// Custom data payload (JSON)
    /// </summary>
    public string? DataJson { get; set; }

    /// <summary>
    /// Whether the notification was sent successfully
    /// </summary>
    [Required]
    public bool Success { get; set; }

    /// <summary>
    /// Firebase message ID (if successful)
    /// </summary>
    [MaxLength(200)]
    public string? FirebaseMessageId { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When the notification was sent (UTC)
    /// </summary>
    [Required]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Platform of the device (Web, iOS, Android)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Platform { get; set; } = "Web";

    /// <summary>
    /// Link to scheduled notification (if from scheduled)
    /// </summary>
    public int? ScheduledNotificationId { get; set; }

    /// <summary>
    /// Navigation property to the scheduled notification
    /// </summary>
    public ScheduledNotification? ScheduledNotification { get; set; }
}
