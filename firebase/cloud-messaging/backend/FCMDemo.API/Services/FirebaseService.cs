using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using FCMDemo.API.Models;
using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Services;

/// <summary>
/// Service for Firebase Cloud Messaging operations using Firebase Admin SDK
/// </summary>
public class FirebaseService : IFirebaseService
{
    private readonly ILogger<FirebaseService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public FirebaseService(
        ILogger<FirebaseService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        InitializeFirebase();
    }

    /// <summary>
    /// Initialize Firebase Admin SDK with credentials
    /// </summary>
    private void InitializeFirebase()
    {
        if (FirebaseApp.DefaultInstance == null)
        {
            var credentialsPath = _configuration["Firebase:CredentialsPath"];

            if (string.IsNullOrEmpty(credentialsPath))
            {
                throw new InvalidOperationException(
                    "Firebase credentials path not configured in appsettings.json");
            }

            // Resolve the full path
            var fullPath = Path.IsPathRooted(credentialsPath)
                ? credentialsPath
                : Path.Combine(AppContext.BaseDirectory, credentialsPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning(
                    "Firebase credentials file not found at: {Path}. " +
                    "Please add firebase-adminsdk.json to the project root. " +
                    "The API will start but notifications will not work until credentials are added.",
                    fullPath);
                return;
            }

            try
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(fullPath),
                    ProjectId = _configuration["Firebase:ProjectId"]
                });

                _logger.LogInformation("Firebase Admin SDK initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK");
                throw;
            }
        }
    }

    /// <summary>
    /// Send a simple notification to a single device
    /// </summary>
    public async Task<NotificationResponse> SendNotificationAsync(SendNotificationRequest request)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                return new NotificationResponse
                {
                    Success = false,
                    Error = "Firebase is not initialized. Please check credentials configuration.",
                    DeviceToken = request.DeviceToken
                };
            }

            var message = new Message
            {
                Token = request.DeviceToken,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body,
                    ImageUrl = request.ImageUrl
                },
                // Web push configuration
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Body,
                        Icon = request.ImageUrl
                    }
                }
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation(
                "Successfully sent notification. Token: {Token}, MessageId: {MessageId}",
                request.DeviceToken[..20] + "...", response);

            // Log the notification to history
            await LogNotificationAsync(request, response, true, null);

            return new NotificationResponse
            {
                Success = true,
                MessageId = response,
                DeviceToken = request.DeviceToken
            };
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase messaging error: {ErrorCode}", ex.MessagingErrorCode);
            var errorMessage = $"Firebase error: {ex.MessagingErrorCode} - {ex.Message}";

            // Log the failed notification
            await LogNotificationAsync(request, null, false, errorMessage);

            return new NotificationResponse
            {
                Success = false,
                Error = errorMessage,
                DeviceToken = request.DeviceToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification");

            // Log the failed notification
            await LogNotificationAsync(request, null, false, ex.Message);

            return new NotificationResponse
            {
                Success = false,
                Error = ex.Message,
                DeviceToken = request.DeviceToken
            };
        }
    }

    /// <summary>
    /// Send a notification with custom data payload
    /// </summary>
    public async Task<NotificationResponse> SendNotificationWithDataAsync(SendNotificationRequest request)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                return new NotificationResponse
                {
                    Success = false,
                    Error = "Firebase is not initialized. Please check credentials configuration.",
                    DeviceToken = request.DeviceToken
                };
            }

            var message = new Message
            {
                Token = request.DeviceToken,
                Notification = new Notification
                {
                    Title = request.Title,
                    Body = request.Body
                },
                Data = request.Data ?? new Dictionary<string, string>(),
                // Web push configuration with custom data
                Webpush = new WebpushConfig
                {
                    Notification = new WebpushNotification
                    {
                        Title = request.Title,
                        Body = request.Body
                    },
                    FcmOptions = new WebpushFcmOptions
                    {
                        Link = request.Data?.ContainsKey("url") == true ? request.Data["url"] : null
                    }
                }
            };

            string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);

            _logger.LogInformation(
                "Successfully sent notification with data. Token: {Token}, MessageId: {MessageId}",
                request.DeviceToken[..20] + "...", response);

            // Log the notification to history
            await LogNotificationAsync(request, response, true, null);

            return new NotificationResponse
            {
                Success = true,
                MessageId = response,
                DeviceToken = request.DeviceToken
            };
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogError(ex, "Firebase messaging error: {ErrorCode}", ex.MessagingErrorCode);
            var errorMessage = $"Firebase error: {ex.MessagingErrorCode} - {ex.Message}";

            // Log the failed notification
            await LogNotificationAsync(request, null, false, errorMessage);

            return new NotificationResponse
            {
                Success = false,
                Error = errorMessage,
                DeviceToken = request.DeviceToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification with data");

            // Log the failed notification
            await LogNotificationAsync(request, null, false, ex.Message);

            return new NotificationResponse
            {
                Success = false,
                Error = ex.Message,
                DeviceToken = request.DeviceToken
            };
        }
    }

    /// <summary>
    /// Validate an FCM token by sending a dry-run message
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("Firebase not initialized, skipping token validation");
                return true; // Allow registration even without Firebase configured
            }

            var message = new Message
            {
                Token = token,
                Notification = new Notification
                {
                    Title = "Validation",
                    Body = "Token validation"
                }
            };

            // Send with dry run to validate token without actually sending
            await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            return true;
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning(ex, "Token validation failed: {ErrorCode}", ex.MessagingErrorCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return false;
        }
    }

    /// <summary>
    /// Log notification to history database
    /// </summary>
    private async Task LogNotificationAsync(
        SendNotificationRequest request,
        string? messageId,
        bool success,
        string? errorMessage)
    {
        try
        {
            // Skip logging if no UserId provided
            if (string.IsNullOrEmpty(request.UserId))
            {
                return;
            }

            // Create a scope to get the history service (avoid circular dependency)
            using var scope = _serviceProvider.CreateScope();
            var historyService = scope.ServiceProvider.GetService<INotificationHistoryService>();

            if (historyService == null)
            {
                _logger.LogWarning("NotificationHistoryService not available, skipping logging");
                return;
            }

            var log = new NotificationLog
            {
                UserId = request.UserId,
                DeviceToken = request.DeviceToken,
                Title = request.Title,
                Body = request.Body,
                DataJson = request.Data != null && request.Data.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(request.Data)
                    : null,
                Success = success,
                FirebaseMessageId = messageId,
                ErrorMessage = errorMessage,
                SentAt = DateTime.UtcNow,
                Platform = request.Platform
            };

            await historyService.LogNotificationAsync(log);
        }
        catch (Exception ex)
        {
            // Don't fail the notification send if logging fails
            _logger.LogError(ex, "Error logging notification to history");
        }
    }
}
