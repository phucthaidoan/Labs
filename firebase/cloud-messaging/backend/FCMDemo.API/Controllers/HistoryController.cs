using FCMDemo.API.Data.Entities;
using FCMDemo.API.Models;
using FCMDemo.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FCMDemo.API.Controllers;

/// <summary>
/// API endpoints for notification history and statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HistoryController : ControllerBase
{
    private readonly INotificationHistoryService _historyService;
    private readonly ILogger<HistoryController> _logger;

    public HistoryController(
        INotificationHistoryService historyService,
        ILogger<HistoryController> logger)
    {
        _historyService = historyService;
        _logger = logger;
    }

    /// <summary>
    /// Get notification history for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="pageSize">Number of items per page (default 20)</param>
    /// <param name="pageNumber">Page number (1-based, default 1)</param>
    /// <returns>List of notification logs</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<NotificationHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserNotificationHistory(
        string userId,
        [FromQuery] int pageSize = 20,
        [FromQuery] int pageNumber = 1)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving notification history for user {UserId} (page {PageNumber}, size {PageSize})",
                userId, pageNumber, pageSize);

            var logs = await _historyService.GetUserNotificationHistoryAsync(userId, pageSize, pageNumber);

            var response = logs.Select(MapToResponse).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification history for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get notification statistics for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Notification statistics</returns>
    [HttpGet("stats/{userId}")]
    [ProducesResponseType(typeof(NotificationStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotificationStats(string userId)
    {
        try
        {
            _logger.LogInformation("Retrieving notification stats for user {UserId}", userId);

            var stats = await _historyService.GetNotificationStatsAsync(userId);

            var response = new NotificationStatsResponse
            {
                TotalSent = stats.TotalSent,
                SuccessCount = stats.SuccessCount,
                FailedCount = stats.FailedCount,
                SuccessRate = stats.SuccessRate,
                LastSentAt = stats.LastSentAt,
                FirstSentAt = stats.FirstSentAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification stats for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get recent notifications across all users
    /// </summary>
    /// <param name="count">Number of notifications to retrieve (default 20)</param>
    /// <returns>List of recent notifications</returns>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<NotificationHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentNotifications([FromQuery] int count = 20)
    {
        try
        {
            _logger.LogInformation("Retrieving {Count} recent notifications", count);

            var logs = await _historyService.GetRecentNotificationsAsync(count);

            var response = logs.Select(MapToResponse).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent notifications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific notification log by ID
    /// </summary>
    /// <param name="id">Log ID</param>
    /// <returns>Notification log</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(NotificationHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotificationLog(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving notification log {LogId}", id);

            var log = await _historyService.GetNotificationLogByIdAsync(id);

            if (log == null)
            {
                return NotFound(new { error = $"Notification log {id} not found" });
            }

            var response = MapToResponse(log);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification log {LogId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static NotificationHistoryResponse MapToResponse(NotificationLog log)
    {
        Dictionary<string, string>? data = null;
        if (!string.IsNullOrEmpty(log.DataJson))
        {
            try
            {
                data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(log.DataJson);
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return new NotificationHistoryResponse
        {
            Id = log.Id,
            UserId = log.UserId,
            DeviceToken = log.DeviceToken,
            Title = log.Title,
            Body = log.Body,
            Data = data,
            Success = log.Success,
            FirebaseMessageId = log.FirebaseMessageId,
            ErrorMessage = log.ErrorMessage,
            SentAt = log.SentAt,
            Platform = log.Platform,
            ScheduledNotificationId = log.ScheduledNotificationId
        };
    }
}
