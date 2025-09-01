using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;

namespace AuditLogging.Services
{
    /// <summary>
    /// Main audit service that orchestrates audit logging operations
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IEnumerable<IAuditSink> _sinks;
        private readonly ILogger<AuditService> _logger;
        private readonly AuditLoggingOptions _options;
        private readonly IDataProtectionService? _dataProtectionService;
        private readonly IMemoryCache _cache;

        public AuditService(
            IEnumerable<IAuditSink> sinks,
            ILogger<AuditService> logger,
            IOptions<AuditLoggingOptions> options,
            IDataProtectionService? dataProtectionService = null,
            IMemoryCache? cache = null)
        {
            _sinks = sinks;
            _logger = logger;
            _options = options.Value;
            _dataProtectionService = dataProtectionService;
            _cache = cache ?? new MemoryCache(new MemoryCacheOptions());
        }

        public async Task LogEventAsync(AuditEvent auditEvent)
        {
            try
            {
                if (!_options.Enabled)
                {
                    _logger.LogDebug("Audit logging is disabled, skipping event {EventId}", auditEvent.Id);
                    return;
                }

                // Apply data protection if enabled
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    auditEvent = await _dataProtectionService.ApplyRetentionPoliciesAsync(auditEvent);
                }

                // Write to all enabled sinks
                var tasks = _sinks.Select(async sink =>
                {
                    try
                    {
                        await sink.WriteAsync(auditEvent);
                        _logger.LogDebug("Audit event {EventId} written to {SinkType} sink", auditEvent.Id, sink.SinkType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write audit event {EventId} to {SinkType} sink", auditEvent.Id, sink.SinkType);
                        throw;
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogInformation("Audit event {EventId} logged successfully to all sinks", auditEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log audit event {EventId}", auditEvent.Id);
                throw;
            }
        }

        public async Task LogEventsAsync(IEnumerable<AuditEvent> auditEvents)
        {
            try
            {
                if (!_options.Enabled)
                {
                    _logger.LogDebug("Audit logging is disabled, skipping {Count} events", auditEvents.Count());
                    return;
                }

                var eventsList = auditEvents.ToList();
                if (!eventsList.Any()) return;

                // Apply data protection if enabled
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    eventsList = (await _dataProtectionService.PseudonymizeBatchAsync(eventsList)).ToList();
                }

                // Write to all enabled sinks
                var tasks = _sinks.Select(async sink =>
                {
                    try
                    {
                        await sink.WriteBatchAsync(eventsList);
                        _logger.LogDebug("Batch of {Count} audit events written to {SinkType} sink", eventsList.Count, sink.SinkType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to write batch of audit events to {SinkType} sink", sink.SinkType);
                        throw;
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogInformation("Batch of {Count} audit events logged successfully to all sinks", eventsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log batch of audit events");
                throw;
            }
        }

        public async Task LogUserActionAsync(string userId, string actionType, string targetResource, string status, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var auditEvent = new AuditEvent
                {
                    UserId = userId,
                    ActionType = actionType,
                    TargetResource = targetResource,
                    Status = status,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    IP = GetCurrentIPAddress(),
                    SessionId = GetCurrentSessionId(),
                    CorrelationId = GetCurrentCorrelationId(),
                    UserAgent = GetCurrentUserAgent(),
                    Location = GetCurrentLocation(),
                    RiskLevel = AssessRiskLevel(actionType, targetResource, status),
                    ContainsSensitiveData = await AssessSensitiveDataAsync(metadata)
                };

                await LogEventAsync(auditEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user action for user {UserId}", userId);
                throw;
            }
        }

        public async Task LogSystemEventAsync(string actionType, string targetResource, string status, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var auditEvent = new AuditEvent
                {
                    UserId = "SYSTEM",
                    ActionType = actionType,
                    TargetResource = targetResource,
                    Status = status,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    IP = "127.0.0.1",
                    SessionId = "SYSTEM",
                    CorrelationId = GetCurrentCorrelationId(),
                    RiskLevel = "Low",
                    ContainsSensitiveData = await AssessSensitiveDataAsync(metadata)
                };

                await LogEventAsync(auditEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system event {ActionType}", actionType);
                throw;
            }
        }

        public async Task LogSecurityEventAsync(string userId, string actionType, string targetResource, string status, string riskLevel, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var auditEvent = new AuditEvent
                {
                    UserId = userId,
                    ActionType = actionType,
                    TargetResource = targetResource,
                    Status = status,
                    RiskLevel = riskLevel,
                    Metadata = metadata ?? new Dictionary<string, object>(),
                    IP = GetCurrentIPAddress(),
                    SessionId = GetCurrentSessionId(),
                    CorrelationId = GetCurrentCorrelationId(),
                    UserAgent = GetCurrentUserAgent(),
                    Location = GetCurrentLocation(),
                    ContainsSensitiveData = await AssessSensitiveDataAsync(metadata)
                };

                await LogEventAsync(auditEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<AuditEvent>> QueryEventsAsync(AuditEventFilter filter)
        {
            try
            {
                // Try to get from cache first
                var cacheKey = GenerateCacheKey(filter);
                if (_cache.TryGetValue(cacheKey, out IEnumerable<AuditEvent>? cachedResult))
                {
                    _logger.LogDebug("Retrieved {Count} audit events from cache", cachedResult?.Count() ?? 0);
                    return cachedResult ?? Enumerable.Empty<AuditEvent>();
                }

                // Query from operational storage (database sink)
                var operationalSink = _sinks.FirstOrDefault(s => s.SupportsFastQuery);
                if (operationalSink == null)
                {
                    throw new InvalidOperationException("No operational storage sink available");
                }

                var results = await operationalSink.ReadAsync(filter);

                // Cache results for a short period
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.Performance.CacheExpiration,
                    Size = results.Count()
                };

                _cache.Set(cacheKey, results, cacheOptions);

                _logger.LogDebug("Retrieved {Count} audit events from operational storage", results.Count());
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query audit events");
                throw;
            }
        }

        public async Task<long> GetEventCountAsync(AuditEventFilter filter)
        {
            try
            {
                // Try to get from cache first
                var cacheKey = $"count:{GenerateCacheKey(filter)}";
                if (_cache.TryGetValue(cacheKey, out long cachedCount))
                {
                    return cachedCount;
                }

                // Query from operational storage
                var operationalSink = _sinks.FirstOrDefault(s => s.SupportsFastQuery);
                if (operationalSink == null)
                {
                    throw new InvalidOperationException("No operational storage sink available");
                }

                var count = await operationalSink.GetCountAsync(filter);

                // Cache count for a short period
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.Performance.CacheExpiration,
                    Size = 1
                };

                _cache.Set(cacheKey, count, cacheOptions);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event count");
                throw;
            }
        }

        public async Task<long> ArchiveEventsAsync(DateTime cutoffDate)
        {
            try
            {
                var totalArchived = 0L;

                // Archive from operational storage to archival storage
                var operationalSink = _sinks.FirstOrDefault(s => s.SupportsFastQuery);
                var archivalSink = _sinks.FirstOrDefault(s => s.SupportsImmutableStorage);

                if (operationalSink == null || archivalSink == null)
                {
                    throw new InvalidOperationException("Both operational and archival storage sinks are required for archival");
                }

                // Mark events for archival in operational storage
                var archivedCount = await operationalSink.ArchiveEventsAsync(cutoffDate);

                // Move events to archival storage
                var eventsToArchive = await operationalSink.ReadAsync(new AuditEventFilter
                {
                    StartDate = DateTime.MinValue,
                    EndDate = cutoffDate,
                    RetentionCategory = RetentionCategory.Archival
                });

                if (eventsToArchive.Any())
                {
                    await archivalSink.WriteBatchAsync(eventsToArchive);
                    totalArchived = eventsToArchive.Count();
                }

                _logger.LogInformation("Archived {Count} audit events older than {CutoffDate}", totalArchived, cutoffDate);
                return totalArchived;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive audit events");
                throw;
            }
        }

        public async Task<AuditEventStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var cacheKey = $"stats:{startDate:yyyyMMdd}:{endDate:yyyyMMdd}";
                if (_cache.TryGetValue(cacheKey, out AuditEventStatistics? cachedStats))
                {
                    return cachedStats!;
                }

                var filter = new AuditEventFilter
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    MaxResults = int.MaxValue
                };

                var events = await QueryEventsAsync(filter);
                var eventsList = events.ToList();

                var stats = new AuditEventStatistics
                {
                    TotalEvents = eventsList.Count,
                    EventsByActionType = eventsList.GroupBy(e => e.ActionType).ToDictionary(g => g.Key, g => (long)g.Count()),
                    EventsByStatus = eventsList.GroupBy(e => e.Status).ToDictionary(g => g.Key, g => (long)g.Count()),
                    EventsByRiskLevel = eventsList.Where(e => !string.IsNullOrEmpty(e.RiskLevel))
                        .GroupBy(e => e.RiskLevel!).ToDictionary(g => g.Key, g => (long)g.Count()),
                    EventsByUser = eventsList.GroupBy(e => e.UserId).ToDictionary(g => g.Key, g => (long)g.Count()),
                    EventsWithSensitiveData = eventsList.Count(e => e.ContainsSensitiveData),
                    AverageEventsPerDay = eventsList.Count > 0 ? (double)eventsList.Count / (endDate - startDate).TotalDays : 0
                };

                // Cache statistics
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _options.Performance.CacheExpiration,
                    Size = 1
                };

                _cache.Set(cacheKey, stats, cacheOptions);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event statistics");
                throw;
            }
        }

        public async Task<AuditServiceHealth> GetHealthAsync()
        {
            try
            {
                var health = new AuditServiceHealth
                {
                    OverallStatus = "Healthy",
                    LastHealthCheck = DateTime.UtcNow,
                    SinkHealth = new Dictionary<string, SinkHealth>()
                };

                var overallStatus = "Healthy";
                var messages = new List<string>();

                foreach (var sink in _sinks)
                {
                    var sinkHealth = new SinkHealth
                    {
                        SinkId = sink.SinkType,
                        Status = "Healthy",
                        LastSuccessfulOperation = DateTime.UtcNow
                    };

                    try
                    {
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        
                        // Test sink health by performing a simple operation
                        if (sink.SupportsFastQuery)
                        {
                            await sink.GetCountAsync(new AuditEventFilter { MaxResults = 1 });
                        }
                        
                        stopwatch.Stop();
                        sinkHealth.ResponseTimeMs = stopwatch.ElapsedMilliseconds;

                        if (stopwatch.ElapsedMilliseconds > 5000) // 5 seconds threshold
                        {
                            sinkHealth.Status = "Degraded";
                            overallStatus = "Degraded";
                            messages.Add($"Sink {sink.SinkType} is responding slowly ({stopwatch.ElapsedMilliseconds}ms)");
                        }
                    }
                    catch (Exception ex)
                    {
                        sinkHealth.Status = "Unhealthy";
                        sinkHealth.ErrorMessage = ex.Message;
                        overallStatus = "Unhealthy";
                        messages.Add($"Sink {sink.SinkType} is unhealthy: {ex.Message}");
                    }

                    health.SinkHealth[sink.SinkType] = sinkHealth;
                }

                health.OverallStatus = overallStatus;
                health.Messages = messages;

                return health;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit service health");
                return new AuditServiceHealth
                {
                    OverallStatus = "Unhealthy",
                    LastHealthCheck = DateTime.UtcNow,
                    Messages = new List<string> { $"Health check failed: {ex.Message}" }
                };
            }
        }

        private string GenerateCacheKey(AuditEventFilter filter)
        {
            var key = $"audit:{filter.StartDate:yyyyMMdd}:{filter.EndDate:yyyyMMdd}:{filter.UserId}:{filter.ActionType}:{filter.TargetResource}:{filter.Status}:{filter.MaxResults}:{filter.Skip}:{filter.SortBy}:{filter.SortDirection}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(key));
        }

        private string GetCurrentIPAddress()
        {
            // This would typically come from the HTTP context
            // For now, return a placeholder
            return "127.0.0.1";
        }

        private string GetCurrentSessionId()
        {
            // This would typically come from the HTTP context
            // For now, return a placeholder
            return Guid.NewGuid().ToString("N");
        }

        private string? GetCurrentCorrelationId()
        {
            // This would typically come from the HTTP context or distributed tracing
            // For now, return null
            return null;
        }

        private string? GetCurrentUserAgent()
        {
            // This would typically come from the HTTP context
            // For now, return null
            return null;
        }

        private string? GetCurrentLocation()
        {
            // This would typically come from geolocation services
            // For now, return null
            return null;
        }

        private string AssessRiskLevel(string actionType, string targetResource, string status)
        {
            // Simple risk assessment logic
            if (actionType.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
                actionType.Contains("Remove", StringComparison.OrdinalIgnoreCase))
            {
                return "High";
            }

            if (actionType.Contains("Update", StringComparison.OrdinalIgnoreCase) ||
                actionType.Contains("Modify", StringComparison.OrdinalIgnoreCase))
            {
                return "Medium";
            }

            if (status.Contains("Failed", StringComparison.OrdinalIgnoreCase) ||
                status.Contains("Error", StringComparison.OrdinalIgnoreCase))
            {
                return "High";
            }

            return "Low";
        }

        private async Task<bool> AssessSensitiveDataAsync(Dictionary<string, object>? metadata)
        {
            if (metadata == null || !metadata.Any()) return false;

            if (_dataProtectionService != null)
            {
                // Create a dummy audit event to check for sensitive data
                var dummyEvent = new AuditEvent
                {
                    Metadata = metadata
                };

                var sensitiveFields = await _dataProtectionService.IdentifySensitiveFieldsAsync(dummyEvent);
                return sensitiveFields.Any();
            }

            // Simple check for common sensitive patterns
            var sensitivePatterns = new[] { "password", "secret", "key", "token", "ssn", "credit", "bank" };
            return metadata.Any(kvp => 
                sensitivePatterns.Any(pattern => 
                    kvp.Key.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value?.ToString()?.Contains(pattern, StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}
