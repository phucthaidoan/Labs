using Microsoft.AspNetCore.Mvc;
using FCMDemo.API.Services;
using FCMDemo.API.Models;

namespace FCMDemo.API.Controllers;

/// <summary>
/// Controller for sending Firebase Cloud Messaging notifications
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IFirebaseService _firebaseService;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        IFirebaseService firebaseService,
        IDeviceTokenService deviceTokenService,
        ILogger<NotificationsController> logger)
    {
        _firebaseService = firebaseService;
        _deviceTokenService = deviceTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Send a simple notification to a single device
    /// </summary>
    /// <param name="request">Notification details</param>
    /// <returns>Notification send result</returns>
    /// <response code="200">Notification sent successfully</response>
    /// <response code="400">Invalid request or notification send failed</response>
    [HttpPost("send")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid notification send request");
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _firebaseService.SendNotificationAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("Notification send failed: {Error}", response.Error);
                return BadRequest(response);
            }

            _logger.LogInformation("Notification sent successfully. MessageId: {MessageId}", response.MessageId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Send a notification with custom data payload
    /// </summary>
    /// <param name="request">Notification with data payload</param>
    /// <returns>Notification send result</returns>
    /// <response code="200">Notification sent successfully</response>
    /// <response code="400">Invalid request or notification send failed</response>
    [HttpPost("send-with-data")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotificationWithData([FromBody] SendNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid notification with data request");
            return BadRequest(ModelState);
        }

        try
        {
            var response = await _firebaseService.SendNotificationWithDataAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("Notification with data send failed: {Error}", response.Error);
                return BadRequest(response);
            }

            _logger.LogInformation(
                "Notification with data sent successfully. MessageId: {MessageId}",
                response.MessageId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification with data");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Send a notification to a user by user ID
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="request">Notification details (DeviceToken will be looked up)</param>
    /// <returns>Notification send result</returns>
    /// <response code="200">Notification sent successfully</response>
    /// <response code="404">No active device token found for user</response>
    /// <response code="400">Notification send failed</response>
    [HttpPost("send-to-user/{userId}")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendToUser(
        string userId,
        [FromBody] SendNotificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid send to user request");
            return BadRequest(ModelState);
        }

        try
        {
            // Look up user's active device token
            var deviceToken = await _deviceTokenService.GetActiveTokenByUserIdAsync(userId);

            if (deviceToken == null)
            {
                _logger.LogWarning("No active device token found for UserId: {UserId}", userId);
                return NotFound(new { message = $"No active device token found for user: {userId}" });
            }

            // Set the token and userId in the request for logging
            request.DeviceToken = deviceToken.Token;
            request.UserId = userId;

            // Send notification
            var response = await _firebaseService.SendNotificationAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("Notification to user failed: {Error}", response.Error);
                return BadRequest(response);
            }

            _logger.LogInformation(
                "Notification sent to user successfully. UserId: {UserId}, MessageId: {MessageId}",
                userId, response.MessageId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
