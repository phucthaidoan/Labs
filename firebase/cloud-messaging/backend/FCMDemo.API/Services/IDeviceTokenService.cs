using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Services;

/// <summary>
/// Interface for device token management operations
/// </summary>
public interface IDeviceTokenService
{
    /// <summary>
    /// Register a new device token or update existing one
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="token">FCM token</param>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="platform">Platform type (Web, iOS, Android)</param>
    /// <returns>Registered or updated device token entity</returns>
    Task<DeviceToken> RegisterOrUpdateTokenAsync(string userId, string token, string deviceId, string platform);

    /// <summary>
    /// Get active token for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Active device token or null if not found</returns>
    Task<DeviceToken?> GetActiveTokenByUserIdAsync(string userId);

    /// <summary>
    /// Get all tokens for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>List of device tokens</returns>
    Task<List<DeviceToken>> GetUserTokensAsync(string userId);

    /// <summary>
    /// Unregister a device token
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <returns>True if successful, false if not found</returns>
    Task<bool> UnregisterTokenAsync(string deviceId);
}
