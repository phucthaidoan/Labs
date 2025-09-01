namespace AuditLogging.Core.Models
{
    /// <summary>
    /// Request model for exporting audit events
    /// </summary>
    public class ExportRequest
    {
        /// <summary>
        /// Filter criteria for the export
        /// </summary>
        public AuditEventFilter Filter { get; set; } = new();

        /// <summary>
        /// Export format (CSV or JSON)
        /// </summary>
        public ExportFormat Format { get; set; } = ExportFormat.CSV;

        /// <summary>
        /// Whether to include sensitive data in the export
        /// </summary>
        public bool IncludeSensitiveData { get; set; } = false;

        /// <summary>
        /// Whether to pseudonymize sensitive data
        /// </summary>
        public bool PseudonymizeData { get; set; } = true;

        /// <summary>
        /// Custom fields to include in the export
        /// </summary>
        public List<string> IncludeFields { get; set; } = new();

        /// <summary>
        /// Custom fields to exclude from the export
        /// </summary>
        public List<string> ExcludeFields { get; set; } = new();

        /// <summary>
        /// Whether to compress the export file
        /// </summary>
        public bool Compress { get; set; } = false;

        /// <summary>
        /// Encryption key for sensitive exports
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// Notification email for when export is ready
        /// </summary>
        public string? NotificationEmail { get; set; }

        /// <summary>
        /// Priority of the export request
        /// </summary>
        public ExportPriority Priority { get; set; } = ExportPriority.Normal;
    }

    /// <summary>
    /// Supported export formats
    /// </summary>
    public enum ExportFormat
    {
        /// <summary>
        /// Comma-separated values format
        /// </summary>
        CSV = 0,

        /// <summary>
        /// JavaScript Object Notation format
        /// </summary>
        JSON = 1,

        /// <summary>
        /// Excel format (.xlsx)
        /// </summary>
        Excel = 2,

        /// <summary>
        /// PDF format
        /// </summary>
        PDF = 3
    }

    /// <summary>
    /// Export priority levels
    /// </summary>
    public enum ExportPriority
    {
        /// <summary>
        /// Low priority - processed during off-peak hours
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority - processed in order
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority - processed before normal requests
        /// </summary>
        High = 2,

        /// <summary>
        /// Urgent priority - processed immediately
        /// </summary>
        Urgent = 3
    }
}
