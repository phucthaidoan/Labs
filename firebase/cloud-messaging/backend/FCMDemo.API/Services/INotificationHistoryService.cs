using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Services;

/// <summary>
/// Service for managing notification history and logs
/// </summary>
public interface INotificationHistoryService
{
    /// <summary>
    /// Log a notification that was sent
    /// </summary>
    /// <param name="log">Notification log entry</param>
    /// <returns>The created log entry</returns>
    Task<NotificationLog> LogNotificationAsync(NotificationLog log);

    /// <summary>
    /// Get notification history for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <returns>List of notification logs</returns>
    Task<List<NotificationLog>> GetUserNotificationHistoryAsync(string userId, int pageSize = 20, int pageNumber = 1);

    /// <summary>
    /// Get notification statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Notification statistics</returns>
    Task<NotificationStats> GetNotificationStatsAsync(string userId);

    /// <summary>
    /// Get recent notifications across all users
    /// </summary>
    /// <param name="count">Number of notifications to retrieve</param>
    /// <returns>List of recent notifications</returns>
    Task<List<NotificationLog>> GetRecentNotificationsAsync(int count = 20);

    /// <summary>
    /// Get a specific notification log by ID
    /// </summary>
    /// <param name="id">Log ID</param>
    /// <returns>Notification log or null</returns>
    Task<NotificationLog?> GetNotificationLogByIdAsync(int id);
}

/// <summary>
/// Notification statistics for a user
/// </summary>
public class NotificationStats
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
