using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Data.Entities;

/// <summary>
/// Represents a device FCM token stored in the database
/// </summary>
public class DeviceToken
{
    /// <summary>
    /// Unique identifier for the device token record
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Firebase Cloud Messaging token for the device
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// User identifier who owns this device
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Unique device identifier
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Platform type: "Web", "iOS", or "Android"
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the token is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Timestamp when the token was first registered
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the token was last used to send a notification
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Timestamp when the token was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
