using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuditLogging.Core.Models;

namespace AuditLogging.Core.Interfaces
{
    /// <summary>
    /// Main service interface for audit logging operations
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Logs an audit event to all configured sinks
        /// </summary>
        /// <param name="auditEvent">The audit event to log</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogEventAsync(AuditEvent auditEvent);

        /// <summary>
        /// Logs multiple audit events to all configured sinks
        /// </summary>
        /// <param name="auditEvents">The audit events to log</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogEventsAsync(IEnumerable<AuditEvent> auditEvent);

        /// <summary>
        /// Logs a user action with automatic context extraction
        /// </summary>
        /// <param name="userId">User ID performing the action</param>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="targetResource">Target resource or entity</param>
        /// <param name="status">Status of the action</param>
        /// <param name="metadata">Additional metadata</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogUserActionAsync(string userId, string actionType, string targetResource, string status, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Logs a system event
        /// </summary>
        /// <param name="actionType">Type of system action</param>
        /// <param name="targetResource">Target resource or entity</param>
        /// <param name="status">Status of the action</param>
        /// <param name="metadata">Additional metadata</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogSystemEventAsync(string actionType, string targetResource, string status, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Logs a security event
        /// </summary>
        /// <param name="userId">User ID involved in the security event</param>
        /// <param name="actionType">Type of security action</param>
        /// <param name="targetResource">Target resource or entity</param>
        /// <param name="status">Status of the action</param>
        /// <param name="riskLevel">Risk level assessment</param>
        /// <param name="metadata">Additional metadata</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task LogSecurityEventAsync(string userId, string actionType, string targetResource, string status, string riskLevel, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Queries audit events from the operational storage
        /// </summary>
        /// <param name="filter">Filter criteria for the query</param>
        /// <returns>Collection of audit events matching the filter</returns>
        Task<IEnumerable<AuditEvent>> QueryEventsAsync(AuditEventFilter filter);

        /// <summary>
        /// Gets the total count of audit events matching the filter
        /// </summary>
        /// <param name="filter">Filter criteria for the count</param>
        /// <returns>Total count of matching events</returns>
        Task<long> GetEventCountAsync(AuditEventFilter filter);

        /// <summary>
        /// Archives audit events older than the specified date
        /// </summary>
        /// <param name="cutoffDate">Events older than this date will be archived</param>
        /// <returns>Number of events archived</returns>
        Task<long> ArchiveEventsAsync(DateTime cutoffDate);

        /// <summary>
        /// Gets audit event statistics for reporting
        /// </summary>
        /// <param name="startDate">Start date for statistics</param>
        /// <param name="endDate">End date for statistics</param>
        /// <returns>Audit event statistics</returns>
        Task<AuditEventStatistics> GetStatisticsAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// Gets the health status of all configured sinks
        /// </summary>
        /// <returns>Health status information</returns>
        Task<AuditServiceHealth> GetHealthAsync();
    }

    /// <summary>
    /// Statistics for audit events
    /// </summary>
    public class AuditEventStatistics
    {
        /// <summary>
        /// Total number of events in the period
        /// </summary>
        public long TotalEvents { get; set; }

        /// <summary>
        /// Number of events by action type
        /// </summary>
        public Dictionary<string, long> EventsByActionType { get; set; } = new();

        /// <summary>
        /// Number of events by status
        /// </summary>
        public Dictionary<string, long> EventsByStatus { get; set; } = new();

        /// <summary>
        /// Number of events by risk level
        /// </summary>
        public Dictionary<string, long> EventsByRiskLevel { get; set; } = new();

        /// <summary>
        /// Number of events by user
        /// </summary>
        public Dictionary<string, long> EventsByUser { get; set; } = new();

        /// <summary>
        /// Number of events containing sensitive data
        /// </summary>
        public long EventsWithSensitiveData { get; set; }

        /// <summary>
        /// Average events per day
        /// </summary>
        public double AverageEventsPerDay { get; set; }
    }

    /// <summary>
    /// Health status of the audit service
    /// </summary>
    public class AuditServiceHealth
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public string OverallStatus { get; set; } = "Healthy";

        /// <summary>
        /// Health status of individual sinks
        /// </summary>
        public Dictionary<string, SinkHealth> SinkHealth { get; set; } = new();

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Any health check errors or warnings
        /// </summary>
        public List<string> Messages { get; set; } = new();
    }

    /// <summary>
    /// Health status of an individual sink
    /// </summary>
    public class SinkHealth
    {
        /// <summary>
        /// Sink identifier
        /// </summary>
        public string SinkId { get; set; } = string.Empty;

        /// <summary>
        /// Health status
        /// </summary>
        public string Status { get; set; } = "Healthy";

        /// <summary>
        /// Response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Last successful operation timestamp
        /// </summary>
        public DateTime? LastSuccessfulOperation { get; set; }

        /// <summary>
        /// Error message if any
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
