using System.ComponentModel.DataAnnotations;

namespace FCMDemo.API.Models;

/// <summary>
/// Request model for registering a device FCM token
/// </summary>
public class RegisterTokenRequest
{
    /// <summary>
    /// User identifier
    /// </summary>
    [Required(ErrorMessage = "UserId is required")]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Firebase Cloud Messaging token
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Unique device identifier
    /// </summary>
    [Required(ErrorMessage = "DeviceId is required")]
    [MaxLength(200)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Platform type: "Web", "iOS", or "Android"
    /// </summary>
    [Required(ErrorMessage = "Platform is required")]
    [MaxLength(50)]
    public string Platform { get; set; } = string.Empty;
}
