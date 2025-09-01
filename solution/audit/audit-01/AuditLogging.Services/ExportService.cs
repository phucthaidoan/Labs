using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;

namespace AuditLogging.Services
{
    /// <summary>
    /// Service for exporting audit events in various formats
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<ExportService> _logger;
        private readonly AuditLoggingOptions _options;
        private readonly IDataProtectionService? _dataProtectionService;
        private readonly Dictionary<string, ExportStatus> _exportJobs;

        public ExportService(
            IAuditService auditService,
            ILogger<ExportService> logger,
            IOptions<AuditLoggingOptions> options,
            IDataProtectionService? dataProtectionService = null)
        {
            _auditService = auditService;
            _logger = logger;
            _options = options.Value;
            _dataProtectionService = dataProtectionService;
            _exportJobs = new Dictionary<string, ExportStatus>();
        }

        public async Task<ExportResult> ExportAsync(ExportRequest exportRequest)
        {
            try
            {
                var exportId = Guid.NewGuid().ToString("N");
                var exportStatus = new ExportStatus
                {
                    ExportId = exportId,
                    Status = ExportJobStatus.Queued,
                    ProgressPercentage = 0,
                    CurrentStage = "Queued",
                    CreatedAt = DateTime.UtcNow,
                    Request = exportRequest
                };

                _exportJobs[exportId] = exportStatus;

                // Start export in background
                _ = Task.Run(async () => await ProcessExportAsync(exportId, exportRequest));

                return new ExportResult
                {
                    ExportId = exportId,
                    Format = exportRequest.Format,
                    Status = exportStatus,
                    FileName = GenerateFileName(exportRequest.Format),
                    MimeType = GetMimeType(exportRequest.Format)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start export");
                throw;
            }
        }

        public async Task<ExportResult> ExportToCsvAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true)
        {
            var exportRequest = new ExportRequest
            {
                Filter = filter,
                Format = ExportFormat.CSV,
                IncludeSensitiveData = includeSensitiveData,
                PseudonymizeData = pseudonymizeData
            };

            return await ExportAsync(exportRequest);
        }

        public async Task<ExportResult> ExportToJsonAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true, bool prettyPrint = true)
        {
            var exportRequest = new ExportRequest
            {
                Filter = filter,
                Format = ExportFormat.JSON,
                IncludeSensitiveData = includeSensitiveData,
                PseudonymizeData = pseudonymizeData
            };

            return await ExportAsync(exportRequest);
        }

        public async Task<ExportResult> ExportToExcelAsync(AuditEventFilter filter, bool includeSensitiveData = false, bool pseudonymizeData = true)
        {
            var exportRequest = new ExportRequest
            {
                Filter = filter,
                Format = ExportFormat.Excel,
                IncludeSensitiveData = includeSensitiveData,
                PseudonymizeData = pseudonymizeData
            };

            return await ExportAsync(exportRequest);
        }

        public async Task<ExportResult> ExportToPdfAsync(AuditEvent filter, bool includeSensitiveData = false, bool pseudonymizeData = true, string? templatePath = null)
        {
            var exportRequest = new ExportRequest
            {
                Filter = new AuditEventFilter
                {
                    StartDate = filter.Timestamp.Date,
                    EndDate = filter.Timestamp.Date.AddDays(1)
                },
                Format = ExportFormat.PDF,
                IncludeSensitiveData = includeSensitiveData,
                PseudonymizeData = pseudonymizeData
            };

            return await ExportAsync(exportRequest);
        }

        public async Task<ExportStatus> GetExportStatusAsync(string exportId)
        {
            if (_exportJobs.TryGetValue(exportId, out var exportStatus))
            {
                return exportStatus;
            }

            throw new ArgumentException($"Export job {exportId} not found");
        }

        public async Task<Stream> DownloadExportAsync(string exportId)
        {
            var exportStatus = await GetExportStatusAsync(exportId);
            
            if (exportStatus.Status != ExportJobStatus.Completed)
            {
                throw new InvalidOperationException($"Export {exportId} is not completed. Current status: {exportStatus.Status}");
            }

            // For now, return a placeholder stream
            // In a real implementation, this would read from file storage
            var content = $"Export {exportId} completed at {exportStatus.CompletedAt}";
            var bytes = Encoding.UTF8.GetBytes(content);
            return new MemoryStream(bytes);
        }

        public async Task CancelExportAsync(string exportId)
        {
            if (_exportJobs.TryGetValue(exportId, out var exportStatus))
            {
                exportStatus.Status = ExportJobStatus.Cancelled;
                exportStatus.CurrentStage = "Cancelled";
                _logger.LogInformation("Export {ExportId} cancelled", exportId);
            }
            else
            {
                throw new ArgumentException($"Export job {exportId} not found");
            }
        }

        public ExportFormat[] GetSupportedFormats()
        {
            return new[] { ExportFormat.CSV, ExportFormat.JSON, ExportFormat.Excel, ExportFormat.PDF };
        }

        public ExportFormatOptions GetFormatOptions(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => new ExportFormatOptions
                {
                    Format = format,
                    FileExtension = ".csv",
                    MimeType = "text/csv",
                    MaxRecords = _options.ExportService.MaxRecordsPerExport,
                    SupportsCompression = true,
                    SupportsEncryption = false,
                    SupportsPagination = false,
                    SupportsCustomStyling = false,
                    Limitations = new List<string> { "No complex data structures", "Limited formatting options" },
                    Features = new List<string> { "Wide compatibility", "Fast processing", "Small file size" }
                },
                ExportFormat.JSON => new ExportFormatOptions
                {
                    Format = format,
                    FileExtension = ".json",
                    MimeType = "application/json",
                    MaxRecords = _options.ExportService.MaxRecordsPerExport,
                    SupportsCompression = true,
                    SupportsEncryption = true,
                    SupportsPagination = true,
                    SupportsCustomStyling = false,
                    Limitations = new List<string> { "Larger file size than CSV", "Not human-readable for large datasets" },
                    Features = new List<string> { "Structured data", "Metadata support", "Easy to parse" }
                },
                ExportFormat.Excel => new ExportFormatOptions
                {
                    Format = format,
                    FileExtension = ".xlsx",
                    MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    MaxRecords = 1000000, // Excel has row limits
                    SupportsCompression = true,
                    SupportsEncryption = true,
                    SupportsPagination = true,
                    SupportsCustomStyling = true,
                    Limitations = new List<string> { "Row limits", "Complex formatting can increase file size" },
                    Features = new List<string> { "Rich formatting", "Multiple sheets", "Charts and graphs" }
                },
                ExportFormat.PDF => new ExportFormatOptions
                {
                    Format = format,
                    FileExtension = ".pdf",
                    MimeType = "application/pdf",
                    MaxRecords = 100000, // PDF has practical limits
                    SupportsCompression = false,
                    SupportsEncryption = true,
                    SupportsPagination = true,
                    SupportsCustomStyling = true,
                    Limitations = new List<string> { "Large file size", "Limited data manipulation" },
                    Features = new List<string> { "Print-friendly", "Secure", "Professional appearance" }
                },
                _ => throw new ArgumentException($"Unsupported export format: {format}")
            };
        }

        private async Task ProcessExportAsync(string exportId, ExportRequest exportRequest)
        {
            try
            {
                var exportStatus = _exportJobs[exportId];
                exportStatus.Status = ExportJobStatus.Processing;
                exportStatus.CurrentStage = "Processing";
                exportStatus.ProgressPercentage = 10;

                // Retrieve audit events
                exportStatus.CurrentStage = "Retrieving data";
                exportStatus.ProgressPercentage = 20;

                var events = await _auditService.QueryEventsAsync(exportRequest.Filter);
                var eventsList = events.ToList();

                if (!eventsList.Any())
                {
                    exportStatus.Status = ExportJobStatus.Completed;
                    exportStatus.CurrentStage = "Completed - No data";
                    exportStatus.ProgressPercentage = 100;
                    exportStatus.CompletedAt = DateTime.UtcNow;
                    return;
                }

                // Apply data protection if needed
                exportStatus.CurrentStage = "Applying data protection";
                exportStatus.ProgressPercentage = 40;

                if (!exportRequest.IncludeSensitiveData && exportRequest.PseudonymizeData && _dataProtectionService != null)
                {
                    eventsList = (await _dataProtectionService.PseudonymizeBatchAsync(eventsList)).ToList();
                }

                // Generate export content
                exportStatus.CurrentStage = "Generating export";
                exportStatus.ProgressPercentage = 60;

                var exportContent = await GenerateExportContentAsync(eventsList, exportRequest);

                // Save export file
                exportStatus.CurrentStage = "Saving file";
                exportStatus.ProgressPercentage = 80;

                var fileName = await SaveExportFileAsync(exportId, exportContent, exportRequest);

                // Complete export
                exportStatus.Status = ExportJobStatus.Completed;
                exportStatus.CurrentStage = "Completed";
                exportStatus.ProgressPercentage = 100;
                exportStatus.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Export {ExportId} completed successfully. File: {FileName}", exportId, fileName);
            }
            catch (Exception ex)
            {
                var exportStatus = _exportJobs[exportId];
                exportStatus.Status = ExportJobStatus.Failed;
                exportStatus.CurrentStage = "Failed";
                exportStatus.ErrorMessage = ex.Message;
                exportStatus.CompletedAt = DateTime.UtcNow;

                _logger.LogError(ex, "Export {ExportId} failed", exportId);
            }
        }

        private async Task<string> GenerateExportContentAsync(List<AuditEvent> events, ExportRequest exportRequest)
        {
            return exportRequest.Format switch
            {
                ExportFormat.CSV => await GenerateCsvContentAsync(events, exportRequest),
                ExportFormat.JSON => await GenerateJsonContentAsync(events, exportRequest),
                ExportFormat.Excel => await GenerateExcelContentAsync(events, exportRequest),
                ExportFormat.PDF => await GeneratePdfContentAsync(events, exportRequest),
                _ => throw new ArgumentException($"Unsupported export format: {exportRequest.Format}")
            };
        }

        private async Task<string> GenerateCsvContentAsync(List<AuditEvent> events, ExportRequest exportRequest)
        {
            var csv = new StringBuilder();

            // Add headers
            var headers = new[]
            {
                "Id", "Timestamp", "UserId", "ActionType", "TargetResource", "IP", "SessionId", "Status",
                "CorrelationId", "UserAgent", "Location", "RiskLevel", "ContainsSensitiveData", "DataHash"
            };

            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Add data rows
            foreach (var auditEvent in events)
            {
                var row = new[]
                {
                    auditEvent.Id.ToString(),
                    auditEvent.Timestamp.ToString("O"),
                    EscapeCsvField(auditEvent.UserId),
                    EscapeCsvField(auditEvent.ActionType),
                    EscapeCsvField(auditEvent.TargetResource),
                    EscapeCsvField(auditEvent.IP),
                    EscapeCsvField(auditEvent.SessionId),
                    EscapeCsvField(auditEvent.Status),
                    EscapeCsvField(auditEvent.CorrelationId),
                    EscapeCsvField(auditEvent.UserAgent),
                    EscapeCsvField(auditEvent.Location),
                    EscapeCsvField(auditEvent.RiskLevel),
                    auditEvent.ContainsSensitiveData.ToString(),
                    EscapeCsvField(auditEvent.DataHash)
                };

                csv.AppendLine(string.Join(",", row.Select(f => $"\"{f}\"")));
            }

            return csv.ToString();
        }

        private async Task<string> GenerateJsonContentAsync(List<AuditEvent> events, ExportRequest exportRequest)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var exportData = new
            {
                ExportInfo = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Format = exportRequest.Format.ToString(),
                    RecordCount = events.Count,
                    Filter = exportRequest.Filter
                },
                AuditEvents = events
            };

            return JsonSerializer.Serialize(exportData, options);
        }

        private async Task<string> GenerateExcelContentAsync(List<AuditEvent> events, ExportRequest exportRequest)
        {
            // For now, return CSV content as Excel can read CSV files
            // In a real implementation, this would use a library like EPPlus or ClosedXML
            return await GenerateCsvContentAsync(events, exportRequest);
        }

        private async Task<string> GeneratePdfContentAsync(List<AuditEvent> events, ExportRequest exportRequest)
        {
            // For now, return a simple text representation
            // In a real implementation, this would use a library like iText7 or PdfSharp
            var content = new StringBuilder();
            content.AppendLine("AUDIT LOG EXPORT");
            content.AppendLine("================");
            content.AppendLine($"Generated: {DateTime.UtcNow:O}");
            content.AppendLine($"Records: {events.Count}");
            content.AppendLine();

            foreach (var auditEvent in events)
            {
                content.AppendLine($"Event ID: {auditEvent.Id}");
                content.AppendLine($"Timestamp: {auditEvent.Timestamp:O}");
                content.AppendLine($"User: {auditEvent.UserId}");
                content.AppendLine($"Action: {auditEvent.ActionType}");
                content.AppendLine($"Resource: {auditEvent.TargetResource}");
                content.AppendLine($"Status: {auditEvent.Status}");
                content.AppendLine("---");
            }

            return content.ToString();
        }

        private async Task<string> SaveExportFileAsync(string exportId, string content, ExportRequest exportRequest)
        {
            var fileName = $"{exportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{GetFormatOptions(exportRequest.Format).FileExtension}";
            var filePath = Path.Combine(_options.ExportService.ExportStoragePath, fileName);

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            // Write file
            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);

            return fileName;
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return string.Empty;
            
            // Escape quotes and commas
            return field.Replace("\"", "\"\"").Replace(",", ";");
        }

        private string GenerateFileName(ExportFormat format)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var formatOptions = GetFormatOptions(format);
            return $"audit_export_{timestamp}{formatOptions.FileExtension}";
        }

        private string GetMimeType(ExportFormat format)
        {
            return GetFormatOptions(format).MimeType;
        }
    }
}
