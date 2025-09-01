using System;

namespace AuditLogging.Core.Models
{
    /// <summary>
    /// Filter criteria for querying audit events
    /// </summary>
    public class AuditEventFilter
    {
        /// <summary>
        /// Start date for filtering (inclusive)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// End date for filtering (inclusive)
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// User ID to filter by
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Action type to filter by
        /// </summary>
        public string? ActionType { get; set; }

        /// <summary>
        /// Target resource to filter by
        /// </summary>
        public string? TargetResource { get; set; }

        /// <summary>
        /// Status to filter by
        /// </summary>
        public string? Status { get; set; }

        /// <summary>
        /// IP address to filter by
        /// </summary>
        public string? IP { get; set; }

        /// <summary>
        /// Session ID to filter by
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Correlation ID to filter by
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Risk level to filter by
        /// </summary>
        public string? RiskLevel { get; set; }

        /// <summary>
        /// Whether to include events with sensitive data
        /// </summary>
        public bool? ContainsSensitiveData { get; set; }

        /// <summary>
        /// Retention category to filter by
        /// </summary>
        public RetentionCategory? RetentionCategory { get; set; }

        /// <summary>
        /// Maximum number of results to return
        /// </summary>
        public int? MaxResults { get; set; } = 1000;

        /// <summary>
        /// Number of results to skip (for pagination)
        /// </summary>
        public int? Skip { get; set; } = 0;

        /// <summary>
        /// Sort field
        /// </summary>
        public string? SortBy { get; set; } = "Timestamp";

        /// <summary>
        /// Sort direction
        /// </summary>
        public SortDirection SortDirection { get; set; } = SortDirection.Descending;

        /// <summary>
        /// Search term for full-text search across multiple fields
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Metadata key to filter by
        /// </summary>
        public string? MetadataKey { get; set; }

        /// <summary>
        /// Metadata value to filter by
        /// </summary>
        public string? MetadataValue { get; set; }
    }

    /// <summary>
    /// Sort direction for audit event queries
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Ascending order
        /// </summary>
        Ascending = 0,

        /// <summary>
        /// Descending order
        /// </summary>
        Descending = 1
    }
}
