using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Models;

/// <summary>
/// Request model for sending a notification
/// </summary>
public class SendNotificationRequest
{
    /// <summary>
    /// Target device FCM token
    /// </summary>
    [Required(ErrorMessage = "DeviceToken is required")]
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>
    /// User ID for tracking notification history (optional for direct sends)
    /// </summary>
    [MaxLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Notification title
    /// </summary>
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Notification body text
    /// </summary>
    [Required(ErrorMessage = "Body is required")]
    [MaxLength(500)]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Optional image URL for the notification
    /// </summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Optional custom data payload
    /// </summary>
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// Platform (Web, iOS, Android) - defaults to Web
    /// </summary>
    [MaxLength(20)]
    public string Platform { get; set; } = "Web";
}
