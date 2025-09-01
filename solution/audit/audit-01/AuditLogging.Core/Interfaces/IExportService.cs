using System;
using System.IO;
using System.Threading.Tasks;
using AuditLogging.Core.Models;

namespace AuditLogging.Core.Interfaces
{
    /// <summary>
    /// Interface for exporting audit events in various formats
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports audit events based on the export request
        /// </summary>
        /// <param name="exportRequest">Export configuration and filter criteria</param>
        /// <returns>Export result with file information</returns>
        Task<ExportResult> ExportAsync(ExportRequest exportRequest);

        /// <summary>
        /// Exports audit events to CSV format
        /// </summary>
        /// <param name="filter">Filter criteria for the export</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <returns>CSV export result</returns>
        Task<ExportResult> ExportToCsvAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true);

        /// <summary>
        /// Exports audit events to JSON format
        /// </summary>
        /// <param name="filter">Filter criteria for the export</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <param name="prettyPrint">Whether to format JSON with indentation</param>
        /// <returns>JSON export result</returns>
        Task<ExportResult> ExportToJsonAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true, bool prettyPrint = true);

        /// <summary>
        /// Exports audit events to Excel format
        /// </summary>
        /// <param name="filter">Filter criteria for the export</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <returns>Excel export result</returns>
        Task<ExportResult> ExportToExcelAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true);

        /// <summary>
        /// Exports audit events to PDF format
        /// </summary>
        /// <param name="filter">Filter criteria for the export</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <param name="templatePath">Path to PDF template file</param>
        /// <returns>PDF export result</returns>
        Task<ExportResult> ExportToPdfAsync(AuditEvent filter, bool includeSensitiveData = false, bool pseudonymizeData = true, string? templatePath = null);

        /// <summary>
        /// Gets the status of an export job
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>Export status information</returns>
        Task<ExportStatus> GetExportStatusAsync(string exportId);

        /// <summary>
        /// Downloads a completed export file
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>File stream for the export</returns>
        Task<Stream> DownloadExportAsync(string exportId);

        /// <summary>
        /// Cancels an in-progress export job
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>Task representing the asynchronous operation</returns>
        Task CancelExportAsync(string exportId);

        /// <summary>
        /// Gets the list of available export formats
        /// </summary>
        /// <returns>Array of supported export formats</returns>
        ExportFormat[] GetSupportedFormats();

        /// <summary>
        /// Gets export format-specific options and limitations
        /// </summary>
        /// <param name="format">Export format to get options for</param>
        /// <returns>Format options and limitations</returns>
        ExportFormatOptions GetFormatOptions(ExportFormat format);
    }

    /// <summary>
    /// Result of an export operation
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// Unique identifier for the export
        /// </summary>
        public string ExportId { get; set; } = string.Empty;

        /// <summary>
        /// Export format
        /// </summary>
        public ExportFormat Format { get; set; }

        /// <summary>
        /// File name of the export
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// MIME type of the export file
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Export status
        /// </summary>
        public ExportStatus Status { get; set; }

        /// <summary>
        /// Number of records exported
        /// </summary>
        public long RecordCount { get; set; }

        /// <summary>
        /// Export completion timestamp
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if export failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Whether the export contains sensitive data
        /// </summary>
        public bool ContainsSensitiveData { get; set; }

        /// <summary>
        /// Whether sensitive data was pseudonymized
        /// </summary>
        public bool DataPseudonymized { get; set; }

        /// <summary>
        /// Export file checksum for integrity verification
        /// </summary>
        public string? FileChecksum { get; set; }

        /// <summary>
        /// Export file checksum algorithm
        /// </summary>
        public string? ChecksumAlgorithm { get; set; }
    }

    /// <summary>
    /// Status of an export operation
    /// </summary>
    public class ExportStatus
    {
        /// <summary>
        /// Export job identifier
        /// </summary>
        public string ExportId { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the export
        /// </summary>
        public ExportJobStatus Status { get; set; }

        /// <summary>
        /// Progress percentage (0-100)
        /// </summary>
        public int ProgressPercentage { get; set; }

        /// <summary>
        /// Current processing stage
        /// </summary>
        public string CurrentStage { get; set; } = string.Empty;

        /// <summary>
        /// Estimated time remaining
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// Export creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Export completion timestamp
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Error message if export failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Export request details
        /// </summary>
        public ExportRequest Request { get; set; } = new();
    }

    /// <summary>
    /// Export job status values
    /// </summary>
    public enum ExportJobStatus
    {
        /// <summary>
        /// Export job is queued
        /// </summary>
        Queued = 0,

        /// <summary>
        /// Export job is processing
        /// </summary>
        Processing = 1,

        /// <summary>
        /// Export job completed successfully
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Export job failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Export job was cancelled
        /// </summary>
        Cancelled = 4
    }

    /// <summary>
    /// Options and limitations for a specific export format
    /// </summary>
    public class ExportFormatOptions
    {
        /// <summary>
        /// Export format
        /// </summary>
        public ExportFormat Format { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string FileExtension { get; set; } = string.Empty;

        /// <summary>
        /// MIME type
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// Maximum number of records supported
        /// </summary>
        public long? MaxRecords { get; set; }

        /// <summary>
        /// Whether the format supports compression
        /// </summary>
        public bool SupportsCompression { get; set; }

        /// <summary>
        /// Whether the format supports encryption
        /// </summary>
        public bool SupportsEncryption { get; set; }

        /// <summary>
        /// Whether the format supports pagination
        /// </summary>
        public bool SupportsPagination { get; set; }

        /// <summary>
        /// Whether the format supports custom styling
        /// </summary>
        public bool SupportsCustomStyling { get; set; }

        /// <summary>
        /// Format-specific limitations
        /// </summary>
        public List<string> Limitations { get; set; } = new();

        /// <summary>
        /// Format-specific features
        /// </summary>
        public List<string> Features { get; set; } = new();
    }
}
