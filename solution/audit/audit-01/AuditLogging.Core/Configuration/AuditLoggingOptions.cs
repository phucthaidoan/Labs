using System;
using System.Collections.Generic;

namespace AuditLogging.Core.Configuration
{
    /// <summary>
    /// Configuration options for the audit logging system
    /// </summary>
    public class AuditLoggingOptions
    {
        /// <summary>
        /// Configuration section name
        /// </summary>
        public const string SectionName = "AuditLogging";

        /// <summary>
        /// Whether audit logging is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Application name for audit events
        /// </summary>
        public string ApplicationName { get; set; } = "Unknown";

        /// <summary>
        /// Environment name (Development, Staging, Production)
        /// </summary>
        public string Environment { get; set; } = "Development";

        /// <summary>
        /// Default retention policies
        /// </summary>
        public RetentionPolicyOptions RetentionPolicies { get; set; } = new();

        /// <summary>
        /// Database sink configuration
        /// </summary>
        public DatabaseSinkOptions DatabaseSink { get; set; } = new();

        /// <summary>
        /// Blob storage sink configuration
        /// </summary>
        public BlobStorageSinkOptions BlobStorageSink { get; set; } = new();

        /// <summary>
        /// Data protection configuration
        /// </summary>
        public DataProtectionOptions DataProtection { get; set; } = new();

        /// <summary>
        /// Export service configuration
        /// </summary>
        public ExportServiceOptions ExportService { get; set; } = new();

        /// <summary>
        /// Archival job configuration
        /// </summary>
        public ArchivalJobOptions ArchivalJob { get; set; } = new();

        /// <summary>
        /// Compliance configuration
        /// </summary>
        public ComplianceOptions Compliance { get; set; } = new();

        /// <summary>
        /// Security configuration
        /// </summary>
        public SecurityOptions Security { get; set; } = new();

        /// <summary>
        /// Performance configuration
        /// </summary>
        public PerformanceOptions Performance { get; set; } = new();
    }

    /// <summary>
    /// Retention policy configuration
    /// </summary>
    public class RetentionPolicyOptions
    {
        /// <summary>
        /// Operational logs retention period (fast storage)
        /// </summary>
        public TimeSpan OperationalRetentionPeriod { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Archival logs retention period (immutable storage)
        /// </summary>
        public TimeSpan ArchivalRetentionPeriod { get; set; } = TimeSpan.FromDays(2555); // 7 years

        /// <summary>
        /// Whether to automatically delete expired operational logs
        /// </summary>
        public bool AutoDeleteExpiredLogs { get; set; } = true;

        /// <summary>
        /// Whether to automatically archive old logs
        /// </summary>
        public bool AutoArchiveLogs { get; set; } = true;

        /// <summary>
        /// Archive job frequency
        /// </summary>
        public TimeSpan ArchiveJobFrequency { get; set; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Cleanup job frequency
        /// </summary>
        public TimeSpan CleanupJobFrequency { get; set; } = TimeSpan.FromDays(1);
    }

    /// <summary>
    /// Database sink configuration
    /// </summary>
    public class DatabaseSinkOptions
    {
        /// <summary>
        /// Whether the database sink is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Database connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database provider (SqlServer, PostgreSQL, etc.)
        /// </summary>
        public string Provider { get; set; } = "SqlServer";

        /// <summary>
        /// Table name for audit events
        /// </summary>
        public string TableName { get; set; } = "AuditEvents";

        /// <summary>
        /// Schema name for audit events table
        /// </summary>
        public string SchemaName { get; set; } = "audit";

        /// <summary>
        /// Whether to use transactions for batch operations
        /// </summary>
        public bool UseTransactions { get; set; } = true;

        /// <summary>
        /// Batch size for bulk operations
        /// </summary>
        public int BatchSize { get; set; } = 1000;

        /// <summary>
        /// Command timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Whether to enable query performance logging
        /// </summary>
        public bool EnableQueryLogging { get; set; } = false;
    }

    /// <summary>
    /// Blob storage sink configuration
    /// </summary>
    public class BlobStorageSinkOptions
    {
        /// <summary>
        /// Whether the blob storage sink is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Storage account connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Container name for audit logs
        /// </summary>
        public string ContainerName { get; set; } = "audit-logs";

        /// <summary>
        /// Whether to enable immutable storage
        /// </summary>
        public bool EnableImmutableStorage { get; set; } = true;

        /// <summary>
        /// Immutable storage policy duration
        /// </summary>
        public TimeSpan ImmutablePolicyDuration { get; set; } = TimeSpan.FromDays(2555);

        /// <summary>
        /// Whether to compress files before upload
        /// </summary>
        public bool CompressFiles { get; set; } = true;

        /// <summary>
        /// Compression level (1-9, 9 being highest)
        /// </summary>
        public int CompressionLevel { get; set; } = 6;

        /// <summary>
        /// Whether to encrypt files before upload
        /// </summary>
        public bool EncryptFiles { get; set; } = false;

        /// <summary>
        /// Encryption key for file encryption
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// Blob naming convention
        /// </summary>
        public string BlobNamingConvention { get; set; } = "{year}/{month}/{day}/{timestamp}-{guid}.json.gz";

        /// <summary>
        /// Maximum blob size in bytes
        /// </summary>
        public long MaxBlobSize { get; set; } = 100 * 1024 * 1024; // 100 MB
    }

    /// <summary>
    /// Data protection configuration
    /// </summary>
    public class DataProtectionOptions
    {
        /// <summary>
        /// Whether data protection is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Default encryption key
        /// </summary>
        public string? DefaultEncryptionKey { get; set; }

        /// <summary>
        /// Default hashing algorithm
        /// </summary>
        public string DefaultHashAlgorithm { get; set; } = "SHA256";

        /// <summary>
        /// Whether to pseudonymize sensitive data
        /// </summary>
        public bool EnablePseudonymization { get; set; } = true;

        /// <summary>
        /// Whether to encrypt sensitive data
        /// </summary>
        public bool EnableEncryption { get; set; } = false;

        /// <summary>
        /// Whether to hash data for integrity
        /// </summary>
        public bool EnableHashing { get; set; } = true;

        /// <summary>
        /// Fields that should always be pseudonymized
        /// </summary>
        public List<string> AlwaysPseudonymizeFields { get; set; } = new()
        {
            "Email",
            "PhoneNumber",
            "SocialSecurityNumber",
            "CreditCardNumber",
            "BankAccountNumber"
        };

        /// <summary>
        /// Fields that should never be pseudonymized
        /// </summary>
        public List<string> NeverPseudonymizeFields { get; set; } = new()
        {
            "Id",
            "Timestamp",
            "ActionType",
            "Status"
        };

        /// <summary>
        /// Pseudonymization methods
        /// </summary>
        public List<string> PseudonymizationMethods { get; set; } = new()
        {
            "Hash",
            "Mask",
            "Random",
            "Deterministic"
        };
    }

    /// <summary>
    /// Export service configuration
    /// </summary>
    public class ExportServiceOptions
    {
        /// <summary>
        /// Whether the export service is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum export file size in bytes
        /// </summary>
        public long MaxExportFileSize { get; set; } = 500 * 1024 * 1024; // 500 MB

        /// <summary>
        /// Maximum records per export
        /// </summary>
        public long MaxRecordsPerExport { get; set; } = 1000000; // 1 million

        /// <summary>
        /// Export file storage path
        /// </summary>
        public string ExportStoragePath { get; set; } = "exports";

        /// <summary>
        /// Export file retention period
        /// </summary>
        public TimeSpan ExportFileRetention { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Whether to enable background export processing
        /// </summary>
        public bool EnableBackgroundProcessing { get; set; } = true;

        /// <summary>
        /// Background processing queue size
        /// </summary>
        public int BackgroundQueueSize { get; set; } = 100;

        /// <summary>
        /// Maximum concurrent export jobs
        /// </summary>
        public int MaxConcurrentExports { get; set; } = 5;
    }

    /// <summary>
    /// Archival job configuration
    /// </summary>
    public class ArchivalJobOptions
    {
        /// <summary>
        /// Whether the archival job is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Archival job schedule (cron expression)
        /// </summary>
        public string Schedule { get; set; } = "0 2 * * *"; // Daily at 2 AM

        /// <summary>
        /// Whether to run archival job on startup
        /// </summary>
        public bool RunOnStartup { get; set; } = false;

        /// <summary>
        /// Maximum archival job duration
        /// </summary>
        public TimeSpan MaxJobDuration { get; set; } = TimeSpan.FromHours(2);

        /// <summary>
        /// Whether to enable parallel processing
        /// </summary>
        public bool EnableParallelProcessing { get; set; } = true;

        /// <summary>
        /// Maximum parallel archival tasks
        /// </summary>
        public int MaxParallelTasks { get; set; } = 5;

        /// <summary>
        /// Whether to enable archival job logging
        /// </summary>
        public bool EnableJobLogging { get; set; } = true;
    }

    /// <summary>
    /// Compliance configuration
    /// </summary>
    public class ComplianceOptions
    {
        /// <summary>
        /// Whether GDPR compliance is enabled
        /// </summary>
        public bool GdprComplianceEnabled { get; set; } = true;

        /// <summary>
        /// Whether FCA GIP compliance is enabled
        /// </summary>
        public bool FcaGipComplianceEnabled { get; set; } = true;

        /// <summary>
        /// Data subject rights support
        /// </summary>
        public DataSubjectRightsOptions DataSubjectRights { get; set; } = new();

        /// <summary>
        /// Data breach notification settings
        /// </summary>
        public DataBreachNotificationOptions DataBreachNotification { get; set; } = new();

        /// <summary>
        /// Audit trail requirements
        /// </summary>
        public AuditTrailRequirementsOptions AuditTrailRequirements { get; set; } = new();
    }

    /// <summary>
    /// Data subject rights configuration
    /// </summary>
    public class DataSubjectRightsOptions
    {
        /// <summary>
        /// Whether right to access is supported
        /// </summary>
        public bool SupportRightToAccess { get; set; } = true;

        /// <summary>
        /// Whether right to erasure is supported
        /// </summary>
        public bool SupportRightToErasure { get; set; } = true;

        /// <summary>
        /// Whether right to rectification is supported
        /// </summary>
        public bool SupportRightToRectification { get; set; } = true;

        /// <summary>
        /// Whether right to portability is supported
        /// </summary>
        public bool SupportRightToPortability { get; set; } = true;

        /// <summary>
        /// Maximum time to respond to data subject requests
        /// </summary>
        public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Data breach notification configuration
    /// </summary>
    public class DataBreachNotificationOptions
    {
        /// <summary>
        /// Whether data breach notification is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum time to notify authorities of a breach
        /// </summary>
        public TimeSpan MaxNotificationTime { get; set; } = TimeSpan.FromDays(3);

        /// <summary>
        /// Notification email addresses
        /// </summary>
        public List<string> NotificationEmails { get; set; } = new();

        /// <summary>
        /// Whether to log breach notifications
        /// </summary>
        public bool LogNotifications { get; set; } = true;
    }

    /// <summary>
    /// Audit trail requirements configuration
    /// </summary>
    public class AuditTrailRequirementsOptions
    {
        /// <summary>
        /// Whether audit trail integrity is required
        /// </summary>
        public bool RequireIntegrity { get; set; } = true;

        /// <summary>
        /// Whether audit trail immutability is required
        /// </summary>
        public bool RequireImmutability { get; set; } = true;

        /// <summary>
        /// Whether audit trail encryption is required
        /// </summary>
        public bool RequireEncryption { get; set; } = false;

        /// <summary>
        /// Minimum audit trail retention period
        /// </summary>
        public TimeSpan MinRetentionPeriod { get; set; } = TimeSpan.FromDays(2555);

        /// <summary>
        /// Whether to enable audit trail monitoring
        /// </summary>
        public bool EnableMonitoring { get; set; } = true;
    }

    /// <summary>
    /// Security configuration
    /// </summary>
    public class SecurityOptions
    {
        /// <summary>
        /// Whether authentication is required
        /// </summary>
        public bool RequireAuthentication { get; set; } = true;

        /// <summary>
        /// Whether authorization is required
        /// </summary>
        public bool RequireAuthorization { get; set; } = true;

        /// <summary>
        /// Required roles for audit log access
        /// </summary>
        public List<string> RequiredRoles { get; set; } = new()
        {
            "Admin",
            "ComplianceOfficer",
            "Auditor"
        };

        /// <summary>
        /// Whether to enable audit log access logging
        /// </summary>
        public bool LogAccessToAuditLogs { get; set; } = true;

        /// <summary>
        /// Whether to enable IP address logging
        /// </summary>
        public bool LogIpAddresses { get; set; } = true;

        /// <summary>
        /// Whether to enable session tracking
        /// </summary>
        public bool EnableSessionTracking { get; set; } = true;

        /// <summary>
        /// Maximum failed authentication attempts
        /// </summary>
        public int MaxFailedAuthAttempts { get; set; } = 5;

        /// <summary>
        /// Account lockout duration
        /// </summary>
        public TimeSpan AccountLockoutDuration { get; set; } = TimeSpan.FromMinutes(15);
    }

    /// <summary>
    /// Performance configuration
    /// </summary>
    public class PerformanceOptions
    {
        /// <summary>
        /// Whether to enable performance monitoring
        /// </summary>
        public bool EnablePerformanceMonitoring { get; set; } = true;

        /// <summary>
        /// Whether to enable caching
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Cache expiration time
        /// </summary>
        public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Maximum cache size in MB
        /// </summary>
        public int MaxCacheSizeMB { get; set; } = 100;

        /// <summary>
        /// Whether to enable async processing
        /// </summary>
        public bool EnableAsyncProcessing { get; set; } = true;

        /// <summary>
        /// Maximum concurrent operations
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 10;

        /// <summary>
        /// Operation timeout
        /// </summary>
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether to enable bulk operations
        /// </summary>
        public bool EnableBulkOperations { get; set; } = true;

        /// <summary>
        /// Bulk operation batch size
        /// </summary>
        public int BulkOperationBatchSize { get; set; } = 1000;
    }
}
