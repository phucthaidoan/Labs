using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Services;

/// <summary>
/// Service for scheduling notifications for future delivery
/// </summary>
public interface INotificationSchedulingService
{
    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    /// <param name="notification">Scheduled notification details</param>
    /// <returns>The created scheduled notification</returns>
    Task<ScheduledNotification> ScheduleNotificationAsync(ScheduledNotification notification);

    /// <summary>
    /// Get all pending scheduled notifications that are due to be sent
    /// </summary>
    /// <returns>List of pending notifications</returns>
    Task<List<ScheduledNotification>> GetPendingNotificationsAsync();

    /// <summary>
    /// Get all scheduled notifications for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of scheduled notifications</returns>
    Task<List<ScheduledNotification>> GetUserScheduledNotificationsAsync(string userId, NotificationStatus? status = null);

    /// <summary>
    /// Get a specific scheduled notification by ID
    /// </summary>
    /// <param name="id">Scheduled notification ID</param>
    /// <returns>Scheduled notification or null</returns>
    Task<ScheduledNotification?> GetScheduledNotificationByIdAsync(int id);

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    /// <param name="id">Scheduled notification ID</param>
    /// <returns>True if cancelled, false if not found or already sent</returns>
    Task<bool> CancelScheduledNotificationAsync(int id);

    /// <summary>
    /// Update scheduled notification status after processing
    /// </summary>
    /// <param name="id">Scheduled notification ID</param>
    /// <param name="status">New status</param>
    /// <param name="errorMessage">Error message if failed</param>
    /// <param name="notificationLogId">Link to notification log if sent</param>
    /// <returns>Updated scheduled notification</returns>
    Task<ScheduledNotification?> UpdateScheduledNotificationStatusAsync(
        int id,
        NotificationStatus status,
        string? errorMessage = null,
        int? notificationLogId = null);

    /// <summary>
    /// Process scheduled notifications that are due to be sent
    /// This method is called by the background job
    /// </summary>
    /// <returns>Number of notifications processed</returns>
    Task<int> ProcessScheduledNotificationsAsync();
}
