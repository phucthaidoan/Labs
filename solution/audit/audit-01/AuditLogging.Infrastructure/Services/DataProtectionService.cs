using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;
using AuditLogging.Infrastructure.Data;

namespace AuditLogging.Infrastructure.Services
{
    /// <summary>
    /// Data protection service for GDPR compliance
    /// </summary>
    public class DataProtectionService : IDataProtectionService
    {
        private readonly AuditLoggingDbContext _context;
        private readonly ILogger<DataProtectionService> _logger;
        private readonly AuditLoggingOptions _options;
        private readonly Dictionary<string, string> _pseudonymizationCache;

        public DataProtectionService(
            AuditLoggingDbContext context,
            ILogger<DataProtectionService> logger,
            IOptions<AuditLoggingOptions> options)
        {
            _context = context;
            _logger = logger;
            _options = options.Value;
            _pseudonymizationCache = new Dictionary<string, string>();
        }

        public async Task<AuditEvent> PseudonymizeAsync(AuditEvent auditEvent)
        {
            try
            {
                var pseudonymizedEvent = auditEvent.CreateArchivalCopy();

                // Identify sensitive fields
                var sensitiveFields = await IdentifySensitiveFieldsAsync(auditEvent);

                // Pseudonymize sensitive fields
                foreach (var field in sensitiveFields)
                {
                    var originalValue = GetFieldValue(auditEvent, field);
                    if (!string.IsNullOrEmpty(originalValue))
                    {
                        var pseudonymizedValue = await PseudonymizeFieldAsync(field, originalValue);
                        SetFieldValue(pseudonymizedEvent, field, pseudonymizedValue);

                        // Store mapping for potential reversal
                        await StorePseudonymizationMappingAsync(field, originalValue, pseudonymizedValue);
                    }
                }

                pseudonymizedEvent.ContainsSensitiveData = true;
                pseudonymizedEvent.DataHash = await GenerateHashAsync(JsonSerializer.Serialize(auditEvent));

                _logger.LogDebug("Pseudonymized audit event {EventId}", auditEvent.Id);
                return pseudonymizedEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pseudonymize audit event {EventId}", auditEvent.Id);
                throw;
            }
        }

        public async Task<IEnumerable<AuditEvent>> PseudonymizeBatchAsync(IEnumerable<AuditEvent> auditEvents)
        {
            var results = new List<AuditEvent>();
            foreach (var auditEvent in auditEvents)
            {
                var pseudonymizedEvent = await PseudonymizeAsync(auditEvent);
                results.Add(pseudonymizedEvent);
            }
            return results;
        }

        public async Task<string> PseudonymizeFieldAsync(string fieldName, string fieldValue, Dictionary<string, object>? context = null)
        {
            try
            {
                // Check cache first
                var cacheKey = $"{fieldName}:{fieldValue}";
                if (_pseudonymizationCache.ContainsKey(cacheKey))
                {
                    return _pseudonymizationCache[cacheKey];
                }

                // Check database for existing mapping
                var existingMapping = await _context.PseudonymizationMappings
                    .FirstOrDefaultAsync(m => m.FieldName == fieldName && m.OriginalValue == fieldValue);

                if (existingMapping != null)
                {
                    _pseudonymizationCache[cacheKey] = existingMapping.PseudonymizedValue;
                    return existingMapping.PseudonymizedValue;
                }

                // Generate new pseudonymized value
                var pseudonymizedValue = GeneratePseudonymizedValue(fieldName, fieldValue, context);

                // Store in cache
                _pseudonymizationCache[cacheKey] = pseudonymizedValue;

                _logger.LogDebug("Pseudonymized field {FieldName} with value {OriginalValue} to {PseudonymizedValue}", 
                    fieldName, fieldValue, pseudonymizedValue);

                return pseudonymizedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pseudonymize field {FieldName} with value {FieldValue}", fieldName, fieldValue);
                throw;
            }
        }

        public async Task<string?> ReversePseudonymizationAsync(string pseudonymizedValue, Dictionary<string, object>? context = null)
        {
            try
            {
                // Check database for mapping
                var mapping = await _context.PseudonymizationMappings
                    .FirstOrDefaultAsync(m => m.PseudonymizedValue == pseudonymizedValue);

                if (mapping == null)
                {
                    _logger.LogWarning("No pseudonymization mapping found for value {PseudonymizedValue}", pseudonymizedValue);
                    return null;
                }

                // Check if mapping has expired
                if (mapping.ExpiresAt.HasValue && mapping.ExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Pseudonymization mapping for value {PseudonymizedValue} has expired", pseudonymizedValue);
                    return null;
                }

                // Check if mapping can be reversed
                if (!mapping.CanBeReversed)
                {
                    _logger.LogWarning("Pseudonymization mapping for value {PseudonymizedValue} cannot be reversed", pseudonymizedValue);
                    return null;
                }

                _logger.LogDebug("Reversed pseudonymization for value {PseudonymizedValue} to {OriginalValue}", 
                    pseudonymizedValue, mapping.OriginalValue);

                return mapping.OriginalValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reverse pseudonymization for value {PseudonymizedValue}", pseudonymizedValue);
                return null;
            }
        }

        public async Task<string> EncryptAsync(string data, string? encryptionKey = null)
        {
            try
            {
                var key = encryptionKey ?? _options.DataProtection.DefaultEncryptionKey;
                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException("No encryption key provided");
                }

                using var aes = Aes.Create();
                var keyBytes = Encoding.UTF8.GetBytes(key);
                var keyHash = SHA256.HashData(keyBytes);
                Array.Resize(ref keyHash, 32); // Ensure 256-bit key

                aes.Key = keyHash;
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);

                // Combine IV and encrypted data
                var result = new byte[aes.IV.Length + encryptedBytes.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

                return Convert.ToBase64String(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw;
            }
        }

        public async Task<string> DecryptAsync(string encryptedData, string? encryptionKey = null)
        {
            try
            {
                var key = encryptionKey ?? _options.DataProtection.DefaultEncryptionKey;
                if (string.IsNullOrEmpty(key))
                {
                    throw new InvalidOperationException("No encryption key provided");
                }

                var encryptedBytes = Convert.FromBase64String(encryptedData);
                if (encryptedBytes.Length < 16)
                {
                    throw new ArgumentException("Invalid encrypted data format");
                }

                using var aes = Aes.Create();
                var keyBytes = Encoding.UTF8.GetBytes(key);
                var keyHash = SHA256.HashData(keyBytes);
                Array.Resize(ref keyHash, 32);

                aes.Key = keyHash;
                aes.IV = new byte[16];
                Buffer.BlockCopy(encryptedBytes, 0, aes.IV, 0, 16);

                using var decryptor = aes.CreateDecryptor();
                var dataBytes = new byte[encryptedBytes.Length - 16];
                Buffer.BlockCopy(encryptedBytes, 16, dataBytes, 0, dataBytes.Length);

                var decryptedBytes = decryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data");
                throw;
            }
        }

        public async Task<string> GenerateHashAsync(string data, AuditLogging.Core.Interfaces.HashAlgorithm algorithm = AuditLogging.Core.Interfaces.HashAlgorithm.SHA256)
        {
            try
            {
                var algorithmName = algorithm switch
                {
                    AuditLogging.Core.Interfaces.HashAlgorithm.MD5 => "MD5",
                    AuditLogging.Core.Interfaces.HashAlgorithm.SHA1 => "SHA1",
                    AuditLogging.Core.Interfaces.HashAlgorithm.SHA256 => "SHA256",
                    AuditLogging.Core.Interfaces.HashAlgorithm.SHA512 => "SHA512",
                    _ => "SHA256"
                };

                using var hashAlgorithm = System.Security.Cryptography.HashAlgorithm.Create(algorithmName);
                if (hashAlgorithm == null)
                {
                    throw new InvalidOperationException($"Hash algorithm {algorithmName} not available");
                }

                var dataBytes = Encoding.UTF8.GetBytes(data);
                var hashBytes = hashAlgorithm.ComputeHash(dataBytes);
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate hash using algorithm {Algorithm}", algorithm);
                throw;
            }
        }

        public async Task<bool> VerifyHashAsync(string data, string hash, AuditLogging.Core.Interfaces.HashAlgorithm algorithm = AuditLogging.Core.Interfaces.HashAlgorithm.SHA256)
        {
            try
            {
                var computedHash = await GenerateHashAsync(data, algorithm);
                return computedHash == hash;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify hash");
                return false;
            }
        }

        public async Task<List<string>> IdentifySensitiveFieldsAsync(AuditEvent auditEvent)
        {
            var sensitiveFields = new List<string>();

            // Check always pseudonymize fields
            foreach (var field in _options.DataProtection.AlwaysPseudonymizeFields)
            {
                var value = GetFieldValue(auditEvent, field);
                if (!string.IsNullOrEmpty(value))
                {
                    sensitiveFields.Add(field);
                }
            }

            // Check metadata for sensitive information
            if (auditEvent.Metadata != null)
            {
                foreach (var kvp in auditEvent.Metadata)
                {
                    if (IsSensitiveMetadataKey(kvp.Key) || IsSensitiveMetadataValue(kvp.Value))
                    {
                        sensitiveFields.Add($"Metadata.{kvp.Key}");
                    }
                }
            }

            // Check for patterns in other fields
            var patterns = new[]
            {
                @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", // Email
                @"\b\d{3}-\d{2}-\d{4}\b", // SSN
                @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", // Credit card
                @"\b\d{10,12}\b" // Phone numbers
            };

            foreach (var field in GetAuditEventFields())
            {
                if (_options.DataProtection.NeverPseudonymizeFields.Contains(field))
                    continue;

                var value = GetFieldValue(auditEvent, field);
                if (!string.IsNullOrEmpty(value) && patterns.Any(p => System.Text.RegularExpressions.Regex.IsMatch(value, p)))
                {
                    sensitiveFields.Add(field);
                }
            }

            return sensitiveFields.Distinct().ToList();
        }

        public async Task<PseudonymizationMapping?> GetPseudonymizationMappingAsync(string pseudonymizedValue)
        {
            try
            {
                return await _context.PseudonymizationMappings
                    .FirstOrDefaultAsync(m => m.PseudonymizedValue == pseudonymizedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pseudonymization mapping for value {PseudonymizedValue}", pseudonymizedValue);
                return null;
            }
        }

        public async Task<AuditEvent> ApplyRetentionPoliciesAsync(AuditEvent auditEvent)
        {
            try
            {
                // Check if event contains sensitive data
                var sensitiveFields = await IdentifySensitiveFieldsAsync(auditEvent);
                auditEvent.ContainsSensitiveData = sensitiveFields.Any();

                // Generate data hash for integrity
                if (_options.DataProtection.EnableHashing)
                {
                    auditEvent.DataHash = await GenerateHashAsync(JsonSerializer.Serialize(auditEvent));
                }

                // Set retention category based on age
                var age = DateTime.UtcNow - auditEvent.Timestamp;
                if (age > _options.RetentionPolicies.OperationalRetentionPeriod)
                {
                    auditEvent.RetentionCategory = RetentionCategory.Archival;
                }

                return auditEvent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply retention policies to audit event {EventId}", auditEvent.Id);
                throw;
            }
        }

        public DataProtectionConfiguration GetConfiguration()
        {
            return new DataProtectionConfiguration
            {
                DefaultEncryptionKey = _options.DataProtection.DefaultEncryptionKey,
                DefaultHashAlgorithm = Enum.Parse<AuditLogging.Core.Interfaces.HashAlgorithm>(_options.DataProtection.DefaultHashAlgorithm),
                PseudonymizationEnabled = _options.DataProtection.EnablePseudonymization,
                EncryptionEnabled = _options.DataProtection.EnableEncryption,
                HashingEnabled = _options.DataProtection.EnableHashing,
                AlwaysPseudonymizeFields = new List<string>(_options.DataProtection.AlwaysPseudonymizeFields),
                NeverPseudonymizeFields = new List<string>(_options.DataProtection.NeverPseudonymizeFields),
                AvailablePseudonymizationMethods = new List<string>(_options.DataProtection.PseudonymizationMethods),
                PseudonymizationMappingRetention = _options.RetentionPolicies.ArchivalRetentionPeriod,
                LogPseudonymizationOperations = true
            };
        }

        private string GeneratePseudonymizedValue(string fieldName, string originalValue, Dictionary<string, object>? context)
        {
            // Use deterministic hashing for consistent pseudonymization
            var salt = $"{fieldName}:{_options.ApplicationName}";
            var dataToHash = $"{salt}:{originalValue}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataToHash));
            var hash = Convert.ToBase64String(hashBytes);
            
            // Take first 16 characters for readability
            return hash.Substring(0, Math.Min(16, hash.Length));
        }

        private async Task StorePseudonymizationMappingAsync(string fieldName, string originalValue, string pseudonymizedValue)
        {
            try
            {
                var mapping = new PseudonymizationMapping
                {
                    OriginalValue = originalValue,
                    PseudonymizedValue = pseudonymizedValue,
                    FieldName = fieldName,
                    PseudonymizedAt = DateTime.UtcNow,
                    Method = "DeterministicHash",
                    CanBeReversed = true,
                    ExpiresAt = DateTime.UtcNow.Add(_options.RetentionPolicies.ArchivalRetentionPeriod),
                    Context = new Dictionary<string, object>
                    {
                        { "ApplicationName", _options.ApplicationName },
                        { "Environment", _options.Environment }
                    }
                };

                _context.PseudonymizationMappings.Add(mapping);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to store pseudonymization mapping for field {FieldName}", fieldName);
            }
        }

        private string? GetFieldValue(AuditEvent auditEvent, string fieldName)
        {
            if (fieldName.StartsWith("Metadata."))
            {
                var metadataKey = fieldName.Substring("Metadata.".Length);
                return auditEvent.Metadata.ContainsKey(metadataKey) ? auditEvent.Metadata[metadataKey]?.ToString() : null;
            }

            return fieldName switch
            {
                "UserId" => auditEvent.UserId,
                "ActionType" => auditEvent.ActionType,
                "TargetResource" => auditEvent.TargetResource,
                "IP" => auditEvent.IP,
                "SessionId" => auditEvent.SessionId,
                "Status" => auditEvent.Status,
                "CorrelationId" => auditEvent.CorrelationId,
                "UserAgent" => auditEvent.UserAgent,
                "Location" => auditEvent.Location,
                "RiskLevel" => auditEvent.RiskLevel,
                _ => null
            };
        }

        private void SetFieldValue(AuditEvent auditEvent, string fieldName, string value)
        {
            if (fieldName.StartsWith("Metadata."))
            {
                var metadataKey = fieldName.Substring("Metadata.".Length);
                auditEvent.Metadata[metadataKey] = value;
                return;
            }

            switch (fieldName)
            {
                case "UserId":
                    auditEvent.UserId = value;
                    break;
                case "ActionType":
                    auditEvent.ActionType = value;
                    break;
                case "TargetResource":
                    auditEvent.TargetResource = value;
                    break;
                case "IP":
                    auditEvent.IP = value;
                    break;
                case "SessionId":
                    auditEvent.SessionId = value;
                    break;
                case "Status":
                    auditEvent.Status = value;
                    break;
                case "CorrelationId":
                    auditEvent.CorrelationId = value;
                    break;
                case "UserAgent":
                    auditEvent.UserAgent = value;
                    break;
                case "Location":
                    auditEvent.Location = value;
                    break;
                case "RiskLevel":
                    auditEvent.RiskLevel = value;
                    break;
            }
        }

        private IEnumerable<string> GetAuditEventFields()
        {
            return new[]
            {
                "UserId", "ActionType", "TargetResource", "IP", "SessionId", "Status",
                "CorrelationId", "UserAgent", "Location", "RiskLevel"
            };
        }

        private bool IsSensitiveMetadataKey(string key)
        {
            var sensitiveKeys = new[] { "email", "phone", "ssn", "creditcard", "bankaccount", "password", "secret" };
            return sensitiveKeys.Any(sk => key.Contains(sk, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsSensitiveMetadataValue(object value)
        {
            if (value is string stringValue)
            {
                var patterns = new[]
                {
                    @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", // Email
                    @"\b\d{3}-\d{2}-\d{4}\b", // SSN
                    @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b" // Credit card
                };

                return patterns.Any(p => System.Text.RegularExpressions.Regex.IsMatch(stringValue, p));
            }

            return false;
        }
    }
}
