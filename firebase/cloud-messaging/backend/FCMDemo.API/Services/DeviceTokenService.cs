using Microsoft.EntityFrameworkCore;
using FCMDemo.API.Data;
using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Services;

/// <summary>
/// Service for managing device FCM tokens
/// </summary>
public class DeviceTokenService : IDeviceTokenService
{
    private readonly FCMDbContext _context;
    private readonly ILogger<DeviceTokenService> _logger;

    public DeviceTokenService(
        FCMDbContext context,
        ILogger<DeviceTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Register a new device token or update if it already exists
    /// </summary>
    public async Task<DeviceToken> RegisterOrUpdateTokenAsync(
        string userId,
        string token,
        string deviceId,
        string platform)
    {
        try
        {
            // Check if device already exists
            var existingToken = await _context.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.DeviceId == deviceId);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.Token = token;
                existingToken.UserId = userId;
                existingToken.Platform = platform;
                existingToken.IsActive = true;
                existingToken.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Updated device token for DeviceId: {DeviceId}, UserId: {UserId}",
                    deviceId, userId);
            }
            else
            {
                // Create new token
                existingToken = new DeviceToken
                {
                    Token = token,
                    UserId = userId,
                    DeviceId = deviceId,
                    Platform = platform,
                    IsActive = true,
                    RegisteredAt = DateTime.UtcNow
                };

                _context.DeviceTokens.Add(existingToken);

                _logger.LogInformation(
                    "Registered new device token for DeviceId: {DeviceId}, UserId: {UserId}",
                    deviceId, userId);
            }

            await _context.SaveChangesAsync();
            return existingToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering/updating device token");
            throw;
        }
    }

    /// <summary>
    /// Get the first active token for a user
    /// </summary>
    public async Task<DeviceToken?> GetActiveTokenByUserIdAsync(string userId)
    {
        try
        {
            return await _context.DeviceTokens
                .Where(dt => dt.UserId == userId && dt.IsActive)
                .OrderByDescending(dt => dt.RegisteredAt)
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active token for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get all tokens for a user (active and inactive)
    /// </summary>
    public async Task<List<DeviceToken>> GetUserTokensAsync(string userId)
    {
        try
        {
            return await _context.DeviceTokens
                .Where(dt => dt.UserId == userId)
                .OrderByDescending(dt => dt.RegisteredAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tokens for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Unregister (deactivate) a device token
    /// </summary>
    public async Task<bool> UnregisterTokenAsync(string deviceId)
    {
        try
        {
            var token = await _context.DeviceTokens
                .FirstOrDefaultAsync(dt => dt.DeviceId == deviceId);

            if (token == null)
            {
                _logger.LogWarning("Device token not found for DeviceId: {DeviceId}", deviceId);
                return false;
            }

            token.IsActive = false;
            token.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Unregistered device token for DeviceId: {DeviceId}", deviceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering device token for DeviceId: {DeviceId}", deviceId);
            throw;
        }
    }
}
