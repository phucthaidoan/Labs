using FCMDemo.API.Models;

namespace FCMDemo.API.Services;

/// <summary>
/// Interface for Firebase Cloud Messaging operations
/// </summary>
public interface IFirebaseService
{
    /// <summary>
    /// Send a simple notification to a device
    /// </summary>
    /// <param name="request">Notification details</param>
    /// <returns>Notification response with success status and message ID</returns>
    Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request);

    /// <summary>
    /// Send a notification with custom data payload to a device
    /// </summary>
    /// <param name="request">Notification details with data</param>
    /// <returns>Notification response</returns>
    Task<NotificationResponse> SendNotificationWithDataAsync(SendNotificationRequest request);

    /// <summary>
    /// Validate if an FCM token is valid
    /// </summary>
    /// <param name="token">FCM token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(string token);
}
