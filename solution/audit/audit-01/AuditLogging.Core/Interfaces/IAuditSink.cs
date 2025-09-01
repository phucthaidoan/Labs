using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuditLogging.Core.Models;

namespace AuditLogging.Core.Interfaces
{
    /// <summary>
    /// Interface for audit event storage sinks
    /// </summary>
    public interface IAuditSink
    {
        /// <summary>
        /// Writes an audit event to the sink
        /// </summary>
        /// <param name="auditEvent">The audit event to write</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task WriteAsync(AuditEvent auditEvent);

        /// <summary>
        /// Writes multiple audit events to the sink
        /// </summary>
        /// <param name="auditEvents">The audit events to write</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents);

        /// <summary>
        /// Reads audit events based on filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria for the query</param>
        /// <returns>Collection of audit events matching the filter</returns>
        Task<IEnumerable<AuditEvent>> ReadAsync(AuditEventFilter filter);

        /// <summary>
        /// Gets the total count of audit events matching the filter
        /// </summary>
        /// <param name="filter">Filter criteria for the count</param>
        /// <returns>Total count of matching events</returns>
        Task<long> GetCountAsync(AuditEventFilter filter);

        /// <summary>
        /// Deletes audit events older than the specified date
        /// </summary>
        /// <param name="cutoffDate">Events older than this date will be deleted</param>
        /// <returns>Number of events deleted</returns>
        Task<long> DeleteOldEventsAsync(DateTime cutoffDate);

        /// <summary>
        /// Archives audit events to long-term storage
        /// </summary>
        /// <param name="cutoffDate">Events older than this date will be archived</param>
        /// <returns>Number of events archived</returns>
        Task<long> ArchiveEventsAsync(DateTime cutoffDate);

        /// <summary>
        /// Gets the sink type identifier
        /// </summary>
        string SinkType { get; }

        /// <summary>
        /// Gets whether the sink supports fast query operations
        /// </summary>
        bool SupportsFastQuery { get; }

        /// <summary>
        /// Gets whether the sink supports immutable storage
        /// </summary>
        bool SupportsImmutableStorage { get; }

        /// <summary>
        /// Gets the maximum retention period supported by this sink
        /// </summary>
        TimeSpan MaxRetentionPeriod { get; }
    }
}
