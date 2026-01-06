using FCMDemo.API.Data;
using FCMDemo.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FCMDemo.API.Services;

/// <summary>
/// Implementation of notification history service
/// </summary>
public class NotificationHistoryService : INotificationHistoryService
{
    private readonly FCMDbContext _context;
    private readonly ILogger<NotificationHistoryService> _logger;

    public NotificationHistoryService(
        FCMDbContext context,
        ILogger<NotificationHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<NotificationLog> LogNotificationAsync(NotificationLog log)
    {
        try
        {
            _context.NotificationLogs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Logged notification: ID={LogId}, User={UserId}, Success={Success}",
                log.Id, log.UserId, log.Success);

            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging notification for user {UserId}", log.UserId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<NotificationLog>> GetUserNotificationHistoryAsync(
        string userId,
        int pageSize = 20,
        int pageNumber = 1)
    {
        try
        {
            var skip = (pageNumber - 1) * pageSize;

            var logs = await _context.NotificationLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.SentAt)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} notification logs for user {UserId} (page {PageNumber})",
                logs.Count, userId, pageNumber);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationStats> GetNotificationStatsAsync(string userId)
    {
        try
        {
            var logs = await _context.NotificationLogs
                .Where(l => l.UserId == userId)
                .ToListAsync();

            var stats = new NotificationStats
            {
                TotalSent = logs.Count,
                SuccessCount = logs.Count(l => l.Success),
                FailedCount = logs.Count(l => !l.Success),
                SuccessRate = logs.Count > 0 ? (logs.Count(l => l.Success) * 100.0 / logs.Count) : 0,
                LastSentAt = logs.Any() ? logs.Max(l => l.SentAt) : null,
                FirstSentAt = logs.Any() ? logs.Min(l => l.SentAt) : null
            };

            _logger.LogInformation(
                "Retrieved stats for user {UserId}: Total={Total}, Success={Success}, Failed={Failed}",
                userId, stats.TotalSent, stats.SuccessCount, stats.FailedCount);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification stats for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<NotificationLog>> GetRecentNotificationsAsync(int count = 20)
    {
        try
        {
            var logs = await _context.NotificationLogs
                .OrderByDescending(l => l.SentAt)
                .Take(count)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} recent notifications", logs.Count);

            return logs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent notifications");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<NotificationLog?> GetNotificationLogByIdAsync(int id)
    {
        try
        {
            var log = await _context.NotificationLogs
                .FirstOrDefaultAsync(l => l.Id == id);

            if (log != null)
            {
                _logger.LogInformation("Retrieved notification log {LogId}", id);
            }
            else
            {
                _logger.LogWarning("Notification log {LogId} not found", id);
            }

            return log;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification log {LogId}", id);
            throw;
        }
    }
}
