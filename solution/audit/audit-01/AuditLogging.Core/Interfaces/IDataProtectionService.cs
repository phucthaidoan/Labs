using AuditLogging.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuditLogging.Core.Interfaces
{
    /// <summary>
    /// Interface for data protection and pseudonymization services
    /// </summary>
    public interface IDataProtectionService
    {
        /// <summary>
        /// Pseudonymizes sensitive data in an audit event
        /// </summary>
        /// <param name="auditEvent">The audit event to pseudonymize</param>
        /// <returns>Pseudonymized audit event</returns>
        Task<AuditEvent> PseudonymizeAsync(AuditEvent auditEvent);

        /// <summary>
        /// Pseudonymizes sensitive data in multiple audit events
        /// </summary>
        /// <param name="auditEvents">The audit events to pseudonymize</param>
        /// <returns>Collection of pseudonymized audit events</returns>
        Task<IEnumerable<AuditEvent>> PseudonymizeBatchAsync(IEnumerable<AuditEvent> auditEvents);

        /// <summary>
        /// Pseudonymizes a specific field value
        /// </summary>
        /// <param name="fieldName">Name of the field to pseudonymize</param>
        /// <param name="fieldValue">Value to pseudonymize</param>
        /// <param name="context">Additional context for pseudonymization</param>
        /// <returns>Pseudonymized value</returns>
        Task<string> PseudonymizeFieldAsync(string fieldName, string fieldValue, Dictionary<string, object>? context = null);

        /// <summary>
        /// Reverses pseudonymization for authorized users
        /// </summary>
        /// <param name="pseudonymizedValue">The pseudonymized value to reverse</param>
        /// <param name="context">Context for reversing the pseudonymization</param>
        /// <returns>Original value if authorized</returns>
        Task<string?> ReversePseudonymizationAsync(string pseudonymizedValue, Dictionary<string, object>? context = null);

        /// <summary>
        /// Encrypts sensitive data
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="encryptionKey">Encryption key (optional, uses default if not provided)</param>
        /// <returns>Encrypted data</returns>
        Task<string> EncryptAsync(string data, string? encryptionKey = null);

        /// <summary>
        /// Decrypts encrypted data
        /// </summary>
        /// <param name="encryptedData">Encrypted data to decrypt</param>
        /// <param name="encryptionKey">Encryption key (optional, uses default if not provided)</param>
        /// <returns>Decrypted data</returns>
        Task<string> DecryptAsync(string encryptedData, string? encryptionKey = null);

        /// <summary>
        /// Generates a hash for data integrity verification
        /// </summary>
        /// <param name="data">Data to hash</param>
        /// <param name="algorithm">Hashing algorithm to use</param>
        /// <returns>Data hash</returns>
        Task<string> GenerateHashAsync(string data, HashAlgorithm algorithm = HashAlgorithm.SHA256);

        /// <summary>
        /// Verifies data integrity using a hash
        /// </summary>
        /// <param name="data">Data to verify</param>
        /// <param name="hash">Expected hash value</param>
        /// <param name="algorithm">Hashing algorithm used</param>
        /// <returns>True if hash matches, false otherwise</returns>
        Task<bool> VerifyHashAsync(string data, string hash, HashAlgorithm algorithm = HashAlgorithm.SHA256);

        /// <summary>
        /// Identifies sensitive fields in an audit event
        /// </summary>
        /// <param name="auditEvent">The audit event to analyze</param>
        /// <returns>List of field names that contain sensitive data</returns>
        Task<List<string>> IdentifySensitiveFieldsAsync(AuditEvent auditEvent);

        /// <summary>
        /// Gets the pseudonymization mapping for a specific value
        /// </summary>
        /// <param name="pseudonymizedValue">The pseudonymized value</param>
        /// <returns>Mapping information if available</returns>
        Task<PseudonymizationMapping?> GetPseudonymizationMappingAsync(string pseudonymizedValue);

        /// <summary>
        /// Applies data retention policies to audit events
        /// </summary>
        /// <param name="auditEvent">The audit event to process</param>
        /// <returns>Processed audit event with retention information</returns>
        Task<AuditEvent> ApplyRetentionPoliciesAsync(AuditEvent auditEvent);

        /// <summary>
        /// Gets the data protection configuration
        /// </summary>
        /// <returns>Data protection configuration</returns>
        DataProtectionConfiguration GetConfiguration();
    }

    /// <summary>
    /// Supported hashing algorithms
    /// </summary>
    public enum HashAlgorithm
    {
        /// <summary>
        /// MD5 hash (not recommended for security)
        /// </summary>
        MD5 = 0,

        /// <summary>
        /// SHA-1 hash (not recommended for security)
        /// </summary>
        SHA1 = 1,

        /// <summary>
        /// SHA-256 hash (recommended)
        /// </summary>
        SHA256 = 2,

        /// <summary>
        /// SHA-512 hash (high security)
        /// </summary>
        SHA512 = 3
    }

    /// <summary>
    /// Pseudonymization mapping information
    /// </summary>
    public class PseudonymizationMapping
    {
        /// <summary>
        /// Original value
        /// </summary>
        public string OriginalValue { get; set; } = string.Empty;

        /// <summary>
        /// Pseudonymized value
        /// </summary>
        public string PseudonymizedValue { get; set; } = string.Empty;

        /// <summary>
        /// Field name
        /// </summary>
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// Pseudonymization timestamp
        /// </summary>
        public DateTime PseudonymizedAt { get; set; }

        /// <summary>
        /// Pseudonymization method used
        /// </summary>
        public string Method { get; set; } = string.Empty;

        /// <summary>
        /// Additional context for the mapping
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new();

        /// <summary>
        /// Whether this mapping can be reversed
        /// </summary>
        public bool CanBeReversed { get; set; }

        /// <summary>
        /// Expiration date for the mapping
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// Data protection configuration
    /// </summary>
    public class DataProtectionConfiguration
    {
        /// <summary>
        /// Default encryption key
        /// </summary>
        public string? DefaultEncryptionKey { get; set; }

        /// <summary>
        /// Default hashing algorithm
        /// </summary>
        public HashAlgorithm DefaultHashAlgorithm { get; set; } = HashAlgorithm.SHA256;

        /// <summary>
        /// Whether pseudonymization is enabled
        /// </summary>
        public bool PseudonymizationEnabled { get; set; } = true;

        /// <summary>
        /// Whether encryption is enabled
        /// </summary>
        public bool EncryptionEnabled { get; set; } = true;

        /// <summary>
        /// Whether hashing is enabled
        /// </summary>
        public bool HashingEnabled { get; set; } = true;

        /// <summary>
        /// Fields that should always be pseudonymized
        /// </summary>
        public List<string> AlwaysPseudonymizeFields { get; set; } = new();

        /// <summary>
        /// Fields that should never be pseudonymized
        /// </summary>
        public List<string> NeverPseudonymizeFields { get; set; } = new();

        /// <summary>
        /// Pseudonymization methods available
        /// </summary>
        public List<string> AvailablePseudonymizationMethods { get; set; } = new();

        /// <summary>
        /// Retention period for pseudonymization mappings
        /// </summary>
        public TimeSpan PseudonymizationMappingRetention { get; set; } = TimeSpan.FromDays(2555); // 7 years

        /// <summary>
        /// Whether to log pseudonymization operations
        /// </summary>
        public bool LogPseudonymizationOperations { get; set; } = true;
    }
}
