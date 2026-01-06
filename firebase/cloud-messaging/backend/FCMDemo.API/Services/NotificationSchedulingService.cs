using FCMDemo.API.Data;
using FCMDemo.API.Data.Entities;
using FCMDemo.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FCMDemo.API.Services;

/// <summary>
/// Implementation of notification scheduling service
/// </summary>
public class NotificationSchedulingService : INotificationSchedulingService
{
    private readonly FCMDbContext _context;
    private readonly IFirebaseService _firebaseService;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly INotificationHistoryService _historyService;
    private readonly ILogger<NotificationSchedulingService> _logger;

    public NotificationSchedulingService(
        FCMDbContext context,
        IFirebaseService firebaseService,
        IDeviceTokenService deviceTokenService,
        INotificationHistoryService historyService,
        ILogger<NotificationSchedulingService> logger)
    {
        _context = context;
        _firebaseService = firebaseService;
        _deviceTokenService = deviceTokenService;
        _historyService = historyService;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ScheduledNotification> ScheduleNotificationAsync(ScheduledNotification notification)
    {
        try
        {
            // Validate scheduled time is in the future
            if (notification.ScheduledFor <= DateTime.UtcNow)
            {
                throw new ArgumentException("Scheduled time must be in the future");
            }

            notification.Status = NotificationStatus.Pending;
            notification.CreatedAt = DateTime.UtcNow;

            _context.ScheduledNotifications.Add(notification);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Scheduled notification {NotificationId} for user {UserId} at {ScheduledFor}",
                notification.Id, notification.UserId, notification.ScheduledFor);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification for user {UserId}", notification.UserId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ScheduledNotification>> GetPendingNotificationsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            var pendingNotifications = await _context.ScheduledNotifications
                .Where(n => n.Status == NotificationStatus.Pending && n.ScheduledFor <= now)
                .OrderBy(n => n.ScheduledFor)
                .ToListAsync();

            _logger.LogInformation("Found {Count} pending notifications due for sending", pendingNotifications.Count);

            return pendingNotifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending notifications");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ScheduledNotification>> GetUserScheduledNotificationsAsync(
        string userId,
        NotificationStatus? status = null)
    {
        try
        {
            var query = _context.ScheduledNotifications
                .Where(n => n.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(n => n.Status == status.Value);
            }

            var notifications = await query
                .OrderByDescending(n => n.ScheduledFor)
                .ToListAsync();

            _logger.LogInformation(
                "Retrieved {Count} scheduled notifications for user {UserId} with status {Status}",
                notifications.Count, userId, status?.ToString() ?? "All");

            return notifications;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled notifications for user {UserId}", userId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ScheduledNotification?> GetScheduledNotificationByIdAsync(int id)
    {
        try
        {
            var notification = await _context.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification != null)
            {
                _logger.LogInformation("Retrieved scheduled notification {NotificationId}", id);
            }
            else
            {
                _logger.LogWarning("Scheduled notification {NotificationId} not found", id);
            }

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled notification {NotificationId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CancelScheduledNotificationAsync(int id)
    {
        try
        {
            var notification = await _context.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                _logger.LogWarning("Cannot cancel - scheduled notification {NotificationId} not found", id);
                return false;
            }

            if (notification.Status != NotificationStatus.Pending)
            {
                _logger.LogWarning(
                    "Cannot cancel - scheduled notification {NotificationId} has status {Status}",
                    id, notification.Status);
                return false;
            }

            notification.Status = NotificationStatus.Cancelled;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled scheduled notification {NotificationId}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling scheduled notification {NotificationId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ScheduledNotification?> UpdateScheduledNotificationStatusAsync(
        int id,
        NotificationStatus status,
        string? errorMessage = null,
        int? notificationLogId = null)
    {
        try
        {
            var notification = await _context.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
            {
                _logger.LogWarning("Scheduled notification {NotificationId} not found", id);
                return null;
            }

            notification.Status = status;
            notification.SentAt = DateTime.UtcNow;
            notification.ErrorMessage = errorMessage;
            notification.NotificationLogId = notificationLogId;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Updated scheduled notification {NotificationId} to status {Status}",
                id, status);

            return notification;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating scheduled notification {NotificationId}", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<int> ProcessScheduledNotificationsAsync()
    {
        try
        {
            var pendingNotifications = await GetPendingNotificationsAsync();

            if (pendingNotifications.Count == 0)
            {
                _logger.LogDebug("No pending notifications to process");
                return 0;
            }

            _logger.LogInformation("Processing {Count} scheduled notifications", pendingNotifications.Count);

            int processedCount = 0;

            foreach (var notification in pendingNotifications)
            {
                try
                {
                    await ProcessSingleNotificationAsync(notification);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing scheduled notification {NotificationId}",
                        notification.Id);

                    // Update as failed
                    await UpdateScheduledNotificationStatusAsync(
                        notification.Id,
                        NotificationStatus.Failed,
                        ex.Message);
                }
            }

            _logger.LogInformation(
                "Processed {ProcessedCount} out of {TotalCount} scheduled notifications",
                processedCount, pendingNotifications.Count);

            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProcessScheduledNotificationsAsync");
            throw;
        }
    }

    private async Task ProcessSingleNotificationAsync(ScheduledNotification notification)
    {
        // Determine the device token to use
        string? deviceToken = notification.DeviceToken;

        if (string.IsNullOrEmpty(deviceToken))
        {
            // No specific token provided, get user's active token
            var userToken = await _deviceTokenService.GetActiveTokenByUserIdAsync(notification.UserId);

            if (userToken == null)
            {
                throw new InvalidOperationException($"No active token found for user {notification.UserId}");
            }

            deviceToken = userToken.Token;
        }

        // Parse data JSON if present
        Dictionary<string, string>? data = null;
        if (!string.IsNullOrEmpty(notification.DataJson))
        {
            try
            {
                data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(notification.DataJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse data JSON for notification {NotificationId}", notification.Id);
            }
        }

        // Send the notification
        var request = new SendNotificationRequest
        {
            DeviceToken = deviceToken,
            Title = notification.Title,
            Body = notification.Body,
            Data = data
        };

        NotificationResponse response;
        if (data != null && data.Count > 0)
        {
            response = await _firebaseService.SendNotificationWithDataAsync(request);
        }
        else
        {
            response = await _firebaseService.SendNotificationAsync(request);
        }

        if (response.Success)
        {
            // Log the notification
            var log = new NotificationLog
            {
                UserId = notification.UserId,
                DeviceToken = deviceToken,
                Title = notification.Title,
                Body = notification.Body,
                DataJson = notification.DataJson,
                Success = true,
                FirebaseMessageId = response.MessageId,
                SentAt = DateTime.UtcNow,
                Platform = "Web",
                ScheduledNotificationId = notification.Id
            };

            var createdLog = await _historyService.LogNotificationAsync(log);

            // Update scheduled notification as sent
            await UpdateScheduledNotificationStatusAsync(
                notification.Id,
                NotificationStatus.Sent,
                notificationLogId: createdLog.Id);

            _logger.LogInformation(
                "Successfully sent scheduled notification {NotificationId} with message ID {MessageId}",
                notification.Id, response.MessageId);
        }
        else
        {
            // Update as failed
            await UpdateScheduledNotificationStatusAsync(
                notification.Id,
                NotificationStatus.Failed,
                response.Error);

            _logger.LogError(
                "Failed to send scheduled notification {NotificationId}: {Error}",
                notification.Id, response.Error);

            throw new Exception($"Failed to send notification: {response.Error}");
        }
    }
}
