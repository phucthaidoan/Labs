using Microsoft.AspNetCore.Mvc;
using FCMDemo.API.Services;
using FCMDemo.API.Models;

namespace FCMDemo.API.Controllers;

/// <summary>
/// Controller for managing device FCM tokens
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly IFirebaseService _firebaseService;
    private readonly ILogger<DeviceTokensController> _logger;

    public DeviceTokensController(
        IDeviceTokenService deviceTokenService,
        IFirebaseService firebaseService,
        ILogger<DeviceTokensController> logger)
    {
        _deviceTokenService = deviceTokenService;
        _firebaseService = firebaseService;
        _logger = logger;
    }

    /// <summary>
    /// Register or update a device FCM token
    /// </summary>
    /// <param name="request">Device token registration details</param>
    /// <returns>Registration result</returns>
    /// <response code="200">Token registered successfully</response>
    /// <response code="400">Invalid request or token validation failed</response>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid token registration request");
            return BadRequest(ModelState);
        }

        try
        {
            // Validate token with Firebase (optional - continues if Firebase not configured)
            var isValid = await _firebaseService.ValidateTokenAsync(request.Token);
            if (!isValid)
            {
                _logger.LogWarning("Invalid FCM token provided");
                return BadRequest(new { message = "Invalid FCM token" });
            }

            var deviceToken = await _deviceTokenService.RegisterOrUpdateTokenAsync(
                request.UserId,
                request.Token,
                request.DeviceId,
                request.Platform);

            _logger.LogInformation(
                "Device token registered successfully. UserId: {UserId}, DeviceId: {DeviceId}",
                request.UserId, request.DeviceId);

            return Ok(new
            {
                message = "Device token registered successfully",
                tokenId = deviceToken.Id,
                userId = deviceToken.UserId,
                deviceId = deviceToken.DeviceId,
                platform = deviceToken.Platform,
                registeredAt = deviceToken.RegisteredAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device token");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Unregister a device token
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>Unregistration result</returns>
    /// <response code="200">Token unregistered successfully</response>
    /// <response code="404">Device token not found</response>
    [HttpDelete("{deviceId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterToken(string deviceId)
    {
        try
        {
            var success = await _deviceTokenService.UnregisterTokenAsync(deviceId);

            if (!success)
            {
                _logger.LogWarning("Device token not found for DeviceId: {DeviceId}", deviceId);
                return NotFound(new { message = $"Device token not found for device: {deviceId}" });
            }

            _logger.LogInformation("Device token unregistered successfully. DeviceId: {DeviceId}", deviceId);

            return Ok(new { message = "Device token unregistered successfully", deviceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all tokens for a specific user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of device tokens</returns>
    /// <response code="200">Returns list of user's tokens</response>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserTokens(string userId)
    {
        try
        {
            var tokens = await _deviceTokenService.GetUserTokensAsync(userId);

            _logger.LogInformation("Retrieved {Count} tokens for UserId: {UserId}", tokens.Count, userId);

            return Ok(new
            {
                userId,
                tokenCount = tokens.Count,
                tokens = tokens.Select(t => new
                {
                    t.Id,
                    t.DeviceId,
                    t.Platform,
                    t.IsActive,
                    t.RegisteredAt,
                    t.LastUsedAt,
                    token = t.Token[..20] + "..." // Truncate token for security
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user tokens");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }
}
