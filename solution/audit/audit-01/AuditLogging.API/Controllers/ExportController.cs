using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;

namespace AuditLogging.API.Controllers
{
    /// <summary>
    /// Controller for audit log export operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,ComplianceOfficer,Auditor")]
    public class ExportController : ControllerBase
    {
        private readonly IExportService _exportService;
        private readonly ILogger<ExportController> _logger;

        public ExportController(IExportService exportService, ILogger<ExportController> logger)
        {
            _exportService = exportService;
            _logger = logger;
        }

        /// <summary>
        /// Exports audit events to CSV format
        /// </summary>
        /// <param name="filter">Export filter criteria</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <returns>Export result</returns>
        [HttpPost("csv")]
        public async Task<IActionResult> ExportToCsv(
            [FromBody] AuditEventFilter filter,
            [FromQuery] bool includeSensitiveData = false,
            [FromQuery] bool pseudonymizeData = true)
        {
            try
            {
                var result = await _exportService.ExportToCsvAsync(filter, includeSensitiveData, pseudonymizeData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export to CSV");
                return StatusCode(500, new { Error = "Failed to export to CSV" });
            }
        }

        /// <summary>
        /// Exports audit events to JSON format
        /// </summary>
        /// <param name="filter">Export filter criteria</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <param name="prettyPrint">Whether to format JSON with indentation</param>
        /// <returns>Export result</returns>
        [HttpPost("json")]
        public async Task<IActionResult> ExportToJson(
            [FromBody] AuditEventFilter filter,
            [FromQuery] bool includeSensitiveData = false,
            [FromQuery] bool pseudonymizeData = true,
            [FromQuery] bool prettyPrint = true)
        {
            try
            {
                var result = await _exportService.ExportToJsonAsync(filter, includeSensitiveData, pseudonymizeData, prettyPrint);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export to JSON");
                return StatusCode(500, new { Error = "Failed to export to JSON" });
            }
        }

        /// <summary>
        /// Exports audit events to Excel format
        /// </summary>
        /// <param name="filter">Export filter criteria</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <returns>Export result</returns>
        [HttpPost("excel")]
        public async Task<IActionResult> ExportToExcel(
            [FromBody] AuditEventFilter filter,
            [FromQuery] bool includeSensitiveData = false,
            [FromQuery] bool pseudonymizeData = true)
        {
            try
            {
                var result = await _exportService.ExportToExcelAsync(filter, includeSensitiveData, pseudonymizeData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export to Excel");
                return StatusCode(500, new { Error = "Failed to export to Excel" });
            }
        }

        /// <summary>
        /// Exports audit events to PDF format
        /// </summary>
        /// <param name="filter">Export filter criteria</param>
        /// <param name="includeSensitiveData">Whether to include sensitive data</param>
        /// <param name="pseudonymizeData">Whether to pseudonymize sensitive data</param>
        /// <param name="templatePath">Path to PDF template file</param>
        /// <returns>Export result</returns>
        [HttpPost("pdf")]
        public async Task<IActionResult> ExportToPdf(
            [FromBody] AuditEventFilter filter,
            [FromQuery] bool includeSensitiveData = false,
            [FromQuery] bool pseudonymizeData = true,
            [FromQuery] string? templatePath = null)
        {
            try
            {
                // Create an export request with the filter
                var exportRequest = new ExportRequest
                {
                    Filter = filter,
                    Format = ExportFormat.PDF,
                    IncludeSensitiveData = includeSensitiveData,
                    PseudonymizeData = pseudonymizeData
                };
                
                var result = await _exportService.ExportAsync(exportRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export to PDF");
                return StatusCode(500, new { Error = "Failed to export to PDF" });
            }
        }

        /// <summary>
        /// Exports audit events using a custom export request
        /// </summary>
        /// <param name="exportRequest">Export configuration and filter criteria</param>
        /// <returns>Export result</returns>
        [HttpPost("custom")]
        public async Task<IActionResult> ExportCustom([FromBody] ExportRequest exportRequest)
        {
            try
            {
                var result = await _exportService.ExportAsync(exportRequest);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to export with custom request");
                return StatusCode(500, new { Error = "Failed to export with custom request" });
            }
        }

        /// <summary>
        /// Gets the status of an export job
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>Export status information</returns>
        [HttpGet("status/{exportId}")]
        public async Task<IActionResult> GetExportStatus(string exportId)
        {
            try
            {
                var status = await _exportService.GetExportStatusAsync(exportId);
                return Ok(status);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get export status for {ExportId}", exportId);
                return StatusCode(500, new { Error = "Failed to get export status" });
            }
        }

        /// <summary>
        /// Downloads a completed export file
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>File stream for the export</returns>
        [HttpGet("download/{exportId}")]
        public async Task<IActionResult> DownloadExport(string exportId)
        {
            try
            {
                var stream = await _exportService.DownloadExportAsync(exportId);
                var status = await _exportService.GetExportStatusAsync(exportId);
                
                var fileName = $"audit_export_{exportId}_{status.CompletedAt:yyyyMMdd_HHmmss}{GetFileExtension(status.Request.Format)}";
                var contentType = GetContentType(status.Request.Format);

                return File(stream, contentType, fileName);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download export {ExportId}", exportId);
                return StatusCode(500, new { Error = "Failed to download export" });
            }
        }

        /// <summary>
        /// Cancels an in-progress export job
        /// </summary>
        /// <param name="exportId">Export job identifier</param>
        /// <returns>Success response</returns>
        [HttpPost("cancel/{exportId}")]
        public async Task<IActionResult> CancelExport(string exportId)
        {
            try
            {
                await _exportService.CancelExportAsync(exportId);
                return Ok(new { Message = "Export cancelled successfully" });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel export {ExportId}", exportId);
                return StatusCode(500, new { Error = "Failed to cancel export" });
            }
        }

        /// <summary>
        /// Gets the list of supported export formats
        /// </summary>
        /// <returns>Array of supported export formats</returns>
        [HttpGet("formats")]
        public IActionResult GetSupportedFormats()
        {
            try
            {
                var formats = _exportService.GetSupportedFormats();
                return Ok(formats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get supported export formats");
                return StatusCode(500, new { Error = "Failed to get supported export formats" });
            }
        }

        /// <summary>
        /// Gets format-specific options and limitations
        /// </summary>
        /// <param name="format">Export format to get options for</param>
        /// <returns>Format options and limitations</returns>
        [HttpGet("formats/{format}/options")]
        public IActionResult GetFormatOptions(ExportFormat format)
        {
            try
            {
                var options = _exportService.GetFormatOptions(format);
                return Ok(options);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get format options for {Format}", format);
                return StatusCode(500, new { Error = "Failed to get format options" });
            }
        }

        private string GetFileExtension(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => ".csv",
                ExportFormat.JSON => ".json",
                ExportFormat.Excel => ".xlsx",
                ExportFormat.PDF => ".pdf",
                _ => ".txt"
            };
        }

        private string GetContentType(ExportFormat format)
        {
            return format switch
            {
                ExportFormat.CSV => "text/csv",
                ExportFormat.JSON => "application/json",
                ExportFormat.Excel => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ExportFormat.PDF => "application/pdf",
                _ => "text/plain"
            };
        }
    }
}
