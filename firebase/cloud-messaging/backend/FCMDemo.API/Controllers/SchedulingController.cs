using FCMDemo.API.Data.Entities;
using FCMDemo.API.Models;
using FCMDemo.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace FCMDemo.API.Controllers;

/// <summary>
/// API endpoints for scheduling notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SchedulingController : ControllerBase
{
    private readonly INotificationSchedulingService _schedulingService;
    private readonly ILogger<SchedulingController> _logger;

    public SchedulingController(
        INotificationSchedulingService schedulingService,
        ILogger<SchedulingController> logger)
    {
        _schedulingService = schedulingService;
        _logger = logger;
    }

    /// <summary>
    /// Schedule a notification for future delivery
    /// </summary>
    /// <param name="request">Scheduled notification details</param>
    /// <returns>Scheduled notification response</returns>
    [HttpPost("schedule")]
    [ProducesResponseType(typeof(ScheduledNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleNotification([FromBody] ScheduleNotificationRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Scheduling notification for user {UserId} at {ScheduledFor}",
                request.UserId, request.ScheduledFor);

            // Convert request to entity
            var notification = new ScheduledNotification
            {
                UserId = request.UserId,
                DeviceToken = request.DeviceToken,
                Title = request.Title,
                Body = request.Body,
                DataJson = request.Data != null && request.Data.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(request.Data)
                    : null,
                ScheduledFor = request.ScheduledFor
            };

            var scheduledNotification = await _schedulingService.ScheduleNotificationAsync(notification);

            var response = MapToResponse(scheduledNotification);

            _logger.LogInformation(
                "Successfully scheduled notification {NotificationId}",
                scheduledNotification.Id);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for scheduling notification");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling notification");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get scheduled notifications for a user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter (Pending, Sent, Failed, Cancelled)</param>
    /// <returns>List of scheduled notifications</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(List<ScheduledNotificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserScheduledNotifications(
        string userId,
        [FromQuery] string? status = null)
    {
        try
        {
            NotificationStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<NotificationStatus>(status, true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var notifications = await _schedulingService.GetUserScheduledNotificationsAsync(userId, statusFilter);

            var response = notifications.Select(MapToResponse).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled notifications for user {UserId}", userId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific scheduled notification by ID
    /// </summary>
    /// <param name="id">Scheduled notification ID</param>
    /// <returns>Scheduled notification</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ScheduledNotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduledNotification(int id)
    {
        try
        {
            var notification = await _schedulingService.GetScheduledNotificationByIdAsync(id);

            if (notification == null)
            {
                return NotFound(new { error = $"Scheduled notification {id} not found" });
            }

            var response = MapToResponse(notification);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving scheduled notification {NotificationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Cancel a scheduled notification
    /// </summary>
    /// <param name="id">Scheduled notification ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelScheduledNotification(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to cancel scheduled notification {NotificationId}", id);

            var success = await _schedulingService.CancelScheduledNotificationAsync(id);

            if (!success)
            {
                return BadRequest(new
                {
                    error = $"Cannot cancel notification {id}. It may not exist or has already been sent."
                });
            }

            _logger.LogInformation("Successfully cancelled scheduled notification {NotificationId}", id);

            return Ok(new { message = $"Scheduled notification {id} cancelled successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling scheduled notification {NotificationId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private static ScheduledNotificationResponse MapToResponse(ScheduledNotification notification)
    {
        return new ScheduledNotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Body = notification.Body,
            ScheduledFor = notification.ScheduledFor,
            Status = notification.Status.ToString(),
            CreatedAt = notification.CreatedAt,
            SentAt = notification.SentAt,
            ErrorMessage = notification.ErrorMessage
        };
    }
}
