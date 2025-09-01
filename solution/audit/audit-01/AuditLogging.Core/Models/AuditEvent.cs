using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AuditLogging.Core.Models
{
    /// <summary>
    /// Represents an audit log entry for GDPR compliance and FCA GIP governance rules
    /// </summary>
    public class AuditEvent
    {
        /// <summary>
        /// Unique identifier for the audit event
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Timestamp when the action occurred (UTC)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// User ID who performed the action
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of action performed
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Target resource or entity that was acted upon
        /// </summary>
        public string TargetResource { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the user
        /// </summary>
        public string IP { get; set; } = string.Empty;

        /// <summary>
        /// Session identifier for the user session
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Status of the action (Success, Failed, Pending, etc.)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata as key-value pairs
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Correlation ID for tracing related events
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// Geographic location if available
        /// </summary>
        public string? Location { get; set; }

        /// <summary>
        /// Risk level assessment (Low, Medium, High, Critical)
        /// </summary>
        public string? RiskLevel { get; set; }

        /// <summary>
        /// Whether this event contains sensitive data that should be pseudonymized
        /// </summary>
        public bool ContainsSensitiveData { get; set; }

        /// <summary>
        /// Hash of sensitive data for integrity verification
        /// </summary>
        public string? DataHash { get; set; }

        /// <summary>
        /// Retention category for archival purposes
        /// </summary>
        public RetentionCategory RetentionCategory { get; set; } = RetentionCategory.Operational;

        /// <summary>
        /// Constructor that sets default values
        /// </summary>
        public AuditEvent()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a copy of the audit event for archival purposes
        /// </summary>
        public AuditEvent CreateArchivalCopy()
        {
            return new AuditEvent
            {
                Id = this.Id,
                Timestamp = this.Timestamp,
                UserId = this.UserId,
                ActionType = this.ActionType,
                TargetResource = this.TargetResource,
                IP = this.IP,
                SessionId = this.SessionId,
                Status = this.Status,
                Metadata = new Dictionary<string, object>(this.Metadata),
                CorrelationId = this.CorrelationId,
                UserAgent = this.UserAgent,
                Location = this.Location,
                RiskLevel = this.RiskLevel,
                ContainsSensitiveData = this.ContainsSensitiveData,
                DataHash = this.DataHash,
                RetentionCategory = RetentionCategory.Archival
            };
        }
    }

    /// <summary>
    /// Retention categories for audit events
    /// </summary>
    public enum RetentionCategory
    {
        /// <summary>
        /// Operational logs kept for 30 days in fast storage
        /// </summary>
        Operational = 0,

        /// <summary>
        /// Archived logs kept for 7 years in immutable storage
        /// </summary>
        Archival = 1
    }
}
