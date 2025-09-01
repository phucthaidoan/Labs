using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;

namespace AuditLogging.Infrastructure.Sinks
{
    /// <summary>
    /// Blob storage sink for immutable long-term storage of audit events
    /// </summary>
    public class BlobStorageSink : IAuditSink
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<BlobStorageSink> _logger;
        private readonly AuditLoggingOptions _options;
        private readonly IDataProtectionService? _dataProtectionService;

        public BlobStorageSink(
            BlobServiceClient blobServiceClient,
            ILogger<BlobStorageSink> logger,
            IOptions<AuditLoggingOptions> options,
            IDataProtectionService? dataProtectionService = null)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
            _options = options.Value;
            _dataProtectionService = dataProtectionService;

            // Get or create container
            _containerClient = _blobServiceClient.GetBlobContainerClient(_options.BlobStorageSink.ContainerName);
            InitializeContainerAsync().Wait();
        }

        public string SinkType => "BlobStorage";

        public bool SupportsFastQuery => false;

        public bool SupportsImmutableStorage => true;

        public TimeSpan MaxRetentionPeriod => _options.RetentionPolicies.ArchivalRetentionPeriod;

        public async Task WriteAsync(AuditEvent auditEvent)
        {
            try
            {
                // Create archival copy
                var archivalEvent = auditEvent.CreateArchivalCopy();

                // Apply data protection if enabled
                if (_dataProtectionService != null && _options.DataProtection.EnablePseudonymization)
                {
                    archivalEvent = await _dataProtectionService.PseudonymizeAsync(archivalEvent);
                }

                // Generate blob name
                var blobName = GenerateBlobName(archivalEvent);

                // Serialize to JSON
                var jsonContent = JsonSerializer.Serialize(archivalEvent, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                // Compress if enabled
                byte[] contentBytes;
                string contentType;
                if (_options.BlobStorageSink.CompressFiles)
                {
                    contentBytes = await CompressContentAsync(jsonContent);
                    contentType = "application/gzip";
                }
                else
                {
                    contentBytes = Encoding.UTF8.GetBytes(jsonContent);
                    contentType = "application/json";
                }

                // Encrypt if enabled
                if (_options.BlobStorageSink.EncryptFiles && !string.IsNullOrEmpty(_options.BlobStorageSink.EncryptionKey))
                {
                    contentBytes = await EncryptContentAsync(contentBytes);
                    contentType = "application/octet-stream";
                }

                // Upload to blob storage
                var blobClient = _containerClient.GetBlobClient(blobName);
                using var stream = new MemoryStream(contentBytes);
                
                var blobHttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType,
                    ContentEncoding = _options.BlobStorageSink.CompressFiles ? "gzip" : null,
                    CacheControl = "no-cache",
                    ContentDisposition = "attachment"
                };

                var metadata = new Dictionary<string, string>
                {
                    { "OriginalId", archivalEvent.Id.ToString() },
                    { "UserId", archivalEvent.UserId },
                    { "ActionType", archivalEvent.ActionType },
                    { "Timestamp", archivalEvent.Timestamp.ToString("O") },
                    { "RetentionCategory", archivalEvent.RetentionCategory.ToString() },
                    { "ContainsSensitiveData", archivalEvent.ContainsSensitiveData.ToString() },
                    { "DataHash", archivalEvent.DataHash ?? string.Empty },
                    { "Compressed", _options.BlobStorageSink.CompressFiles.ToString() },
                    { "Encrypted", _options.BlobStorageSink.EncryptFiles.ToString() },
                    { "UploadTime", DateTime.UtcNow.ToString("O") }
                };

                await blobClient.UploadAsync(stream, blobHttpHeaders, metadata);

                // Set immutable policy if enabled
                if (_options.BlobStorageSink.EnableImmutableStorage)
                {
                    await SetImmutablePolicyAsync(blobClient);
                }

                _logger.LogDebug("Audit event {EventId} archived to blob storage as {BlobName}", 
                    auditEvent.Id, blobName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive audit event {EventId} to blob storage", auditEvent.Id);
                throw;
            }
        }

        public async Task WriteBatchAsync(IEnumerable<AuditEvent> auditEvents)
        {
            try
            {
                var eventsList = auditEvents.ToList();
                if (!eventsList.Any()) return;

                // Process events in parallel for better performance
                var tasks = eventsList.Select(async auditEvent =>
                {
                    try
                    {
                        await WriteAsync(auditEvent);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to archive audit event {EventId} in batch", auditEvent.Id);
                        throw;
                    }
                });

                await Task.WhenAll(tasks);

                _logger.LogDebug("Batch of {Count} audit events archived to blob storage", eventsList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive batch of audit events to blob storage");
                throw;
            }
        }

        public async Task<IEnumerable<AuditEvent>> ReadAsync(AuditEventFilter filter)
        {
            try
            {
                var results = new List<AuditEvent>();
                var cutoffDate = filter.StartDate ?? DateTime.MinValue;
                var endDate = filter.EndDate ?? DateTime.MaxValue;

                // List blobs in date range
                var blobs = _containerClient.GetBlobsAsync(prefix: GetDatePrefix(cutoffDate));

                await foreach (var blobItem in blobs)
                {
                    try
                    {
                        // Parse timestamp from blob name
                        if (TryParseTimestampFromBlobName(blobItem.Name, out var blobTimestamp))
                        {
                            if (blobTimestamp >= cutoffDate && blobTimestamp <= endDate)
                            {
                                var auditEvent = await ReadBlobAsync(blobItem.Name);
                                if (auditEvent != null && MatchesFilter(auditEvent, filter))
                                {
                                    results.Add(auditEvent);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read blob {BlobName}", blobItem.Name);
                    }
                }

                // Apply sorting and pagination
                results = ApplySortingAndPagination(results, filter);

                _logger.LogDebug("Retrieved {Count} audit events from blob storage", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read audit events from blob storage");
                throw;
            }
        }

        public async Task<long> GetCountAsync(AuditEventFilter filter)
        {
            try
            {
                var count = 0L;
                var cutoffDate = filter.StartDate ?? DateTime.MinValue;
                var endDate = filter.EndDate ?? DateTime.MaxValue;

                var blobs = _containerClient.GetBlobsAsync(prefix: GetDatePrefix(cutoffDate));

                await foreach (var blobItem in blobs)
                {
                    if (TryParseTimestampFromBlobName(blobItem.Name, out var blobTimestamp))
                    {
                        if (blobTimestamp >= cutoffDate && blobTimestamp <= endDate)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event count from blob storage");
                throw;
            }
        }

        public async Task<long> DeleteOldEventsAsync(DateTime cutoffDate)
        {
            try
            {
                var deletedCount = 0L;
                var blobs = _containerClient.GetBlobsAsync();

                await foreach (var blobItem in blobs)
                {
                    if (TryParseTimestampFromBlobName(blobItem.Name, out var blobTimestamp))
                    {
                        if (blobTimestamp < cutoffDate)
                        {
                            try
                            {
                                var blobClient = _containerClient.GetBlobClient(blobItem.Name);
                                await blobClient.DeleteAsync();
                                deletedCount++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to delete old blob {BlobName}", blobItem.Name);
                            }
                        }
                    }
                }

                _logger.LogInformation("Deleted {Count} old audit event blobs from storage", deletedCount);
                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old audit events from blob storage");
                throw;
            }
        }

        public async Task<long> ArchiveEventsAsync(DateTime cutoffDate)
        {
            // This method is not applicable for blob storage sink
            // Blob storage is already the archival destination
            _logger.LogInformation("ArchiveEventsAsync called on blob storage sink - not applicable");
            return 0;
        }

        private async Task InitializeContainerAsync()
        {
            try
            {
                await _containerClient.CreateIfNotExistsAsync();

                // Set container metadata
                var metadata = new Dictionary<string, string>
                {
                    { "Purpose", "Audit Log Archival" },
                    { "RetentionPeriod", _options.RetentionPolicies.ArchivalRetentionPeriod.TotalDays.ToString() },
                    { "ImmutableStorage", _options.BlobStorageSink.EnableImmutableStorage.ToString() },
                    { "Compression", _options.BlobStorageSink.CompressFiles.ToString() },
                    { "Encryption", _options.BlobStorageSink.EncryptFiles.ToString() },
                    { "CreatedAt", DateTime.UtcNow.ToString("O") }
                };

                await _containerClient.SetMetadataAsync(metadata);

                _logger.LogInformation("Blob storage container {ContainerName} initialized", _containerClient.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize blob storage container {ContainerName}", _containerClient.Name);
                throw;
            }
        }

        private string GenerateBlobName(AuditEvent auditEvent)
        {
            var timestamp = auditEvent.Timestamp;
            var year = timestamp.Year.ToString("D4");
            var month = timestamp.Month.ToString("D2");
            var day = timestamp.Day.ToString("D2");
            var time = timestamp.ToString("HHmmss");
            var guid = auditEvent.Id.ToString("N");

            return $"{year}/{month}/{day}/{timestamp:yyyyMMdd}-{time}-{guid}.json.gz";
        }

        private string GetDatePrefix(DateTime date)
        {
            var year = date.Year.ToString("D4");
            var month = date.Month.ToString("D2");
            var day = date.Day.ToString("D2");
            return $"{year}/{month}/{day}/";
        }

        private bool TryParseTimestampFromBlobName(string blobName, out DateTime timestamp)
        {
            timestamp = DateTime.MinValue;
            try
            {
                // Extract date from blob name format: yyyy/mm/dd/yyyyMMdd-HHmmss-guid.json.gz
                var parts = blobName.Split('/');
                if (parts.Length >= 4)
                {
                    var datePart = parts[3].Split('-')[0]; // yyyyMMdd
                    if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                    {
                        timestamp = parsedDate;
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<byte[]> CompressContentAsync(string content)
        {
            using var outputStream = new MemoryStream();
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress, true))
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                await gzipStream.WriteAsync(contentBytes);
            }
            return outputStream.ToArray();
        }

        private async Task<byte[]> EncryptContentAsync(byte[] content)
        {
            if (_dataProtectionService == null)
            {
                throw new InvalidOperationException("Data protection service is required for encryption");
            }

            var contentString = Convert.ToBase64String(content);
            var encryptedString = await _dataProtectionService.EncryptAsync(contentString, _options.BlobStorageSink.EncryptionKey);
            return Encoding.UTF8.GetBytes(encryptedString);
        }

        private async Task SetImmutablePolicyAsync(BlobClient blobClient)
        {
            try
            {
                var policy = new BlobImmutabilityPolicy
                {
                    ExpiresOn = DateTimeOffset.UtcNow.Add(_options.BlobStorageSink.ImmutablePolicyDuration),
                    PolicyMode = BlobImmutabilityPolicyMode.Unlocked
                };

                await blobClient.SetImmutabilityPolicyAsync(policy);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set immutable policy for blob {BlobName}", blobClient.Name);
            }
        }

        private async Task<AuditEvent?> ReadBlobAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                var response = await blobClient.DownloadAsync();

                using var stream = response.Value.Content;
                using var reader = new StreamReader(stream);
                var content = await reader.ReadToEndAsync();

                // Decrypt if needed
                if (_options.BlobStorageSink.EncryptFiles && !string.IsNullOrEmpty(_options.BlobStorageSink.EncryptionKey))
                {
                    content = await DecryptContentAsync(content);
                }

                // Decompress if needed
                if (_options.BlobStorageSink.CompressFiles)
                {
                    content = await DecompressContentAsync(content);
                }

                // Deserialize JSON
                var auditEvent = JsonSerializer.Deserialize<AuditEvent>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                return auditEvent;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read blob {BlobName}", blobName);
                return null;
            }
        }

        private async Task<string> DecryptContentAsync(string encryptedContent)
        {
            if (_dataProtectionService == null)
            {
                throw new InvalidOperationException("Data protection service is required for decryption");
            }

            return await _dataProtectionService.DecryptAsync(encryptedContent, _options.BlobStorageSink.EncryptionKey);
        }

        private async Task<string> DecompressContentAsync(string compressedContent)
        {
            var compressedBytes = Convert.FromBase64String(compressedContent);
            using var inputStream = new MemoryStream(compressedBytes);
            using var outputStream = new MemoryStream();
            using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);

            await gzipStream.CopyToAsync(outputStream);
            return Encoding.UTF8.GetString(outputStream.ToArray());
        }

        private bool MatchesFilter(AuditEvent auditEvent, AuditEventFilter filter)
        {
            if (!string.IsNullOrEmpty(filter.UserId) && auditEvent.UserId != filter.UserId)
                return false;

            if (!string.IsNullOrEmpty(filter.ActionType) && auditEvent.ActionType != filter.ActionType)
                return false;

            if (!string.IsNullOrEmpty(filter.TargetResource) && auditEvent.TargetResource != filter.TargetResource)
                return false;

            if (!string.IsNullOrEmpty(filter.Status) && auditEvent.Status != filter.Status)
                return false;

            if (filter.ContainsSensitiveData.HasValue && auditEvent.ContainsSensitiveData != filter.ContainsSensitiveData.Value)
                return false;

            return true;
        }

        private List<AuditEvent> ApplySortingAndPagination(List<AuditEvent> results, AuditEventFilter filter)
        {
            // Apply sorting
            var sortBy = filter.SortBy?.ToLower() ?? "timestamp";
            var sortDirection = filter.SortDirection;

            results = sortBy switch
            {
                "userid" => sortDirection == SortDirection.Ascending 
                    ? results.OrderBy(e => e.UserId).ToList() 
                    : results.OrderByDescending(e => e.UserId).ToList(),
                "actiontype" => sortDirection == SortDirection.Ascending 
                    ? results.OrderBy(e => e.ActionType).ToList() 
                    : results.OrderByDescending(e => e.ActionType).ToList(),
                "targetresource" => sortDirection == SortDirection.Ascending 
                    ? results.OrderBy(e => e.TargetResource).ToList() 
                    : results.OrderByDescending(e => e.TargetResource).ToList(),
                "status" => sortDirection == SortDirection.Ascending 
                    ? results.OrderBy(e => e.Status).ToList() 
                    : results.OrderByDescending(e => e.Status).ToList(),
                _ => sortDirection == SortDirection.Ascending 
                    ? results.OrderBy(e => e.Timestamp).ToList() 
                    : results.OrderByDescending(e => e.Timestamp).ToList()
            };

            // Apply pagination
            if (filter.Skip.HasValue)
            {
                results = results.Skip(filter.Skip.Value).ToList();
            }

            if (filter.MaxResults.HasValue)
            {
                results = results.Take(filter.MaxResults.Value).ToList();
            }

            return results;
        }
    }
}
