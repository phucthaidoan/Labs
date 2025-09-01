using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;
using AuditLogging.Infrastructure.Data;

namespace AuditLogging.Infrastructure.Sinks
{
    /// <summary>
    /// Database sink for storing audit events in a relational database
    /// </summary>
    public class DatabaseSink : IAuditSink
    {
        private readonly AuditLoggingDbContext _context;
        private readonly ILogger<DatabaseSink> _logger;
        private readonly AuditLoggingOptions _options;
        private readonly IDataProtectionService? _dataProtectionService;

        public DatabaseSink(
            AuditLoggingDbContext context,
            ILogger<DatabaseSink> logger,
            IOptions<AuditLoggingOptions> options,
            IDataProtectionService? dataProtectionService = null)
        {
            _context = context;
            _logger = logger;
            _options = options.Value;
            _dataProtectionService = dataProtectionService;
        }

        public string SinkType => "Database";

        public bool SupportsFastQuery => true;

        public bool SupportsImmutableStorage => false;

        public TimeSpan MaxRetentionPeriod => _options.RetentionPolicies.OperationalRetentionPeriod;

        public async Task WriteAsync(AuditEvent auditEvent)
        {
            try
            {
                // Apply data protection if enabled
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    auditEvent = await _dataProtectionService.PseudonymizeAsync(auditEvent);
                }

                // Set retention category to operational for database storage
                auditEvent.RetentionCategory = RetentionCategory.Operational;

                // Generate data hash if enabled
                if (_options.DataProtection.EnableHashing)
                {
                    auditEvent.DataHash = await GenerateDataHashAsync(auditEvent);
                }

                _context.AuditEvents.Add(auditEvent);
                await _context.SaveChangesAsync();

                _logger.LogDebug("Audit event {EventId} written to database", auditEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write audit event {EventId} to database", auditEvent.Id);
                throw;
            }
        }

        public async Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents)
        {
            try
            {
                var eventsList = auditEvents.ToList();
                if (!eventsList.Any()) return;

                // Apply data protection if enabled
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    eventsList = (await _dataProtectionService.PseudonymizeBatchAsync(eventsList)).ToList();
                }

                // Set retention category and generate hashes
                foreach (var auditEvent in eventsList)
                {
                    auditEvent.RetentionCategory = RetentionCategory.Operational;
                    
                    if (_options.DataProtection.EnableHashing)
                    {
                        auditEvent.DataHash = await GenerateDataHashAsync(auditEvent);
                    }
                }

                // Use bulk insert for better performance
                if (_options.DatabaseSink.UseTransactions)
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        await _context.AuditEvents.AddRangeAsync(eventsList);
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
                else
                {
                    await _context.AuditEvents.AddRangeAsync(eventsList);
                    await _context.SaveChangesAsync();
                }

                _logger.LogDebug("Batch of {Count} audit events written to database", eventsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write batch of audit events to database");
                throw;
            }
        }

        public async Task<IEnumerable<AuditEvent>> ReadAsync(AuditEventFilter filter)
        {
            try
            {
                var query = _context.AuditEvents.AsQueryable();

                // Apply filters
                query = ApplyFilters(query, filter);

                // Apply sorting
                query = ApplySorting(query, filter);

                // Apply pagination
                if (filter.Skip.HasValue)
                {
                    query = query.Skip(filter.Skip.Value);
                }

                if (filter.MaxResults.HasValue)
                {
                    query = query.Take(filter.MaxResults.Value);
                }

                var results = await query.ToListAsync();

                // Reverse pseudonymization if needed and authorized
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    // This would require authorization context
                    // For now, we'll return pseudonymized data
                }

                _logger.LogDebug("Retrieved {Count} audit events from database", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read audit events from database");
                throw;
            }
        }

        public async Task<long> GetCountAsync(AuditEventFilter filter)
        {
            try
            {
                var query = _context.AuditEvents.AsQueryable();
                query = ApplyFilters(query, filter);
                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event count from database");
                throw;
            }
        }

        public async Task<long> DeleteOldEventsAsync(DateTime cutoffDate)
        {
            try
            {
                var oldEvents = await _context.AuditEvents
                    .Where(e => e.Timestamp < cutoffDate && e.RetentionCategory == RetentionCategory.Operational)
                    .ToListAsync();

                if (!oldEvents.Any()) return 0;

                _context.AuditEvents.RemoveRange(oldEvents);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {Count} old audit events from database", oldEvents.Count);
                return oldEvents.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old audit events from database");
                throw;
            }
        }

        public async Task<long> ArchiveEventsAsync(DateTime cutoffDate)
        {
            try
            {
                var eventsToArchive = await _context.AuditEvents
                    .Where(e => e.Timestamp < cutoffDate && e.RetentionCategory == RetentionCategory.Operational)
                    .ToListAsync();

                if (!eventsToArchive.Any()) return 0;

                // Mark events for archival
                foreach (var auditEvent in eventsToArchive)
                {
                    auditEvent.RetentionCategory = RetentionCategory.Archival;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Marked {Count} audit events for archival", eventsToArchive.Count);
                return eventsToArchive.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive audit events in database");
                throw;
            }
        }

        private IQueryable<AuditEvent> ApplyFilters(IQueryable<AuditEvent> query, AuditEventFilter filter)
        {
            if (filter.StartDate.HasValue)
            {
                query = query.Where(e => e.Timestamp >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(e => e.Timestamp <= filter.EndDate.Value);
            }

            if (!string.IsNullOrEmpty(filter.UserId))
            {
                query = query.Where(e => e.UserId == filter.UserId);
            }

            if (!string.IsNullOrEmpty(filter.ActionType))
            {
                query = query.Where(e => e.ActionType == filter.ActionType);
            }

            if (!string.IsNullOrEmpty(filter.TargetResource))
            {
                query = query.Where(e => e.TargetResource == filter.TargetResource);
            }

            if (!string.IsNullOrEmpty(filter.Status))
            {
                query = query.Where(e => e.Status == filter.Status);
            }

            if (!string.IsNullOrEmpty(filter.IP))
            {
                query = query.Where(e => e.IP == filter.IP);
            }

            if (!string.IsNullOrEmpty(filter.SessionId))
            {
                query = query.Where(e => e.SessionId == filter.SessionId);
            }

            if (!string.IsNullOrEmpty(filter.CorrelationId))
            {
                query = query.Where(e => e.CorrelationId == filter.CorrelationId);
            }

            if (!string.IsNullOrEmpty(filter.RiskLevel))
            {
                query = query.Where(e => e.RiskLevel == filter.RiskLevel);
            }

            if (filter.ContainsSensitiveData.HasValue)
            {
                query = query.Where(e => e.ContainsSensitiveData == filter.ContainsSensitiveData.Value);
            }

            if (filter.RetentionCategory.HasValue)
            {
                query = query.Where(e => e.RetentionCategory == filter.RetentionCategory.Value);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(e =>
                    e.UserId.ToLower().Contains(searchTerm) ||
                    e.ActionType.ToLower().Contains(searchTerm) ||
                    e.TargetResource.ToLower().Contains(searchTerm) ||
                    e.Status.ToLower().Contains(searchTerm) ||
                    e.CorrelationId != null && e.CorrelationId.ToLower().Contains(searchTerm)
                );
            }

            if (!string.IsNullOrEmpty(filter.MetadataKey) && !string.IsNullOrEmpty(filter.MetadataValue))
            {
                // For JSON metadata search, we'd need to use EF.Functions.JsonContains
                // This is a simplified version
                query = query.Where(e => e.Metadata.ContainsKey(filter.MetadataKey));
            }

            return query;
        }

        private IQueryable<AuditEvent> ApplySorting(IQueryable<AuditEvent> query, AuditEventFilter filter)
        {
            var sortBy = filter.SortBy?.ToLower() ?? "timestamp";
            var sortDirection = filter.SortDirection;

            query = sortBy switch
            {
                "userid" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.UserId) 
                    : query.OrderByDescending(e => e.UserId),
                "actiontype" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.ActionType) 
                    : query.OrderByDescending(e => e.ActionType),
                "targetresource" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.TargetResource) 
                    : query.OrderByDescending(e => e.TargetResource),
                "status" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.Status) 
                    : query.OrderByDescending(e => e.Status),
                "ip" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.IP) 
                    : query.OrderByDescending(e => e.IP),
                "sessionid" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.SessionId) 
                    : query.OrderByDescending(e => e.SessionId),
                "correlationid" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.CorrelationId) 
                    : query.OrderByDescending(e => e.CorrelationId),
                "risklevel" => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.RiskLevel) 
                    : query.OrderByDescending(e => e.RiskLevel),
                _ => sortDirection == SortDirection.Ascending 
                    ? query.OrderBy(e => e.Timestamp) 
                    : query.OrderByDescending(e => e.Timestamp)
            };

            return query;
        }

        private async Task<string> GenerateDataHashAsync(AuditEvent auditEvent)
        {
            if (_dataProtectionService == null) return string.Empty;

            try
            {
                var dataToHash = $"{auditEvent.UserId}{auditEvent.ActionType}{auditEvent.TargetResource}{auditEvent.Status}{auditEvent.Timestamp:yyyyMMddHHmmss}";
                return await _dataProtectionService.GenerateHashAsync(dataToHash);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate data hash for audit event {EventId}", auditEvent.Id);
                return string.Empty;
            }
        }
    }
}
