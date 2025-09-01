using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuditLogging.Core.Interfaces;
using AuditLogging.Core.Models;

namespace AuditLogging.API.Controllers
{
    /// <summary>
    /// Controller for audit logging operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,ComplianceOfficer,Auditor")]
    public class AuditController : ControllerBase
    {
        private readonly IAuditService _auditService;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditService auditService, ILogger<AuditController> logger)
        {
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Logs a user action
        /// </summary>
        /// <param name="request">User action details</param>
        /// <returns>Success response</returns>
        [HttpPost("user-action")]
        public async Task<IActionResult> LogUserAction([FromBody] LogUserActionRequest request)
        {
            try
            {
                await _auditService.LogUserActionAsync(
                    request.UserId,
                    request.ActionType,
                    request.TargetResource,
                    request.Status,
                    request.Metadata);

                return Ok(new { Message = "User action logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log user action");
                return StatusCode(500, new { Error = "Failed to log user action" });
            }
        }

        /// <summary>
        /// Logs a system event
        /// </summary>
        /// <param name="request">System event details</param>
        /// <returns>Success response</returns>
        [HttpPost("system-event")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LogSystemEvent([FromBody] LogSystemEventRequest request)
        {
            try
            {
                await _auditService.LogSystemEventAsync(
                    request.ActionType,
                    request.TargetResource,
                    request.Status,
                    request.Metadata);

                return Ok(new { Message = "System event logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log system event");
                return StatusCode(500, new { Error = "Failed to log system event" });
            }
        }

        /// <summary>
        /// Logs a security event
        /// </summary>
        /// <param name="request">Security event details</param>
        /// <returns>Success response</returns>
        [HttpPost("security-event")]
        [Authorize(Roles = "Admin,ComplianceOfficer")]
        public async Task<IActionResult> LogSecurityEvent([FromBody] LogSecurityEventRequest request)
        {
            try
            {
                await _auditService.LogSecurityEventAsync(
                    request.UserId,
                    request.ActionType,
                    request.TargetResource,
                    request.Status,
                    request.RiskLevel,
                    request.Metadata);

                return Ok(new { Message = "Security event logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event");
                return StatusCode(500, new { Error = "Failed to log security event" });
            }
        }

        /// <summary>
        /// Queries audit events
        /// </summary>
        /// <param name="filter">Query filter criteria</param>
        /// <returns>Collection of audit events</returns>
        [HttpGet("events")]
        public async Task<IActionResult> QueryEvents([FromQuery] AuditEventFilter filter)
        {
            try
            {
                var events = await _auditService.QueryEventsAsync(filter);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query audit events");
                return StatusCode(500, new { Error = "Failed to query audit events" });
            }
        }

        /// <summary>
        /// Gets the count of audit events
        /// </summary>
        /// <param name="filter">Query filter criteria</param>
        /// <returns>Count of audit events</returns>
        [HttpGet("events/count")]
        public async Task<IActionResult> GetEventCount([FromQuery] AuditEventFilter filter)
        {
            try
            {
                var count = await _auditService.GetEventCountAsync(filter);
                return Ok(new { Count = count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event count");
                return StatusCode(500, new { Error = "Failed to get audit event count" });
            }
        }

        /// <summary>
        /// Gets audit event statistics
        /// </summary>
        /// <param name="startDate">Start date for statistics</param>
        /// <param name="endDate">End date for statistics</param>
        /// <returns>Audit event statistics</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var statistics = await _auditService.GetStatisticsAsync(startDate, endDate);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit event statistics");
                return StatusCode(500, new { Error = "Failed to get audit event statistics" });
            }
        }

        /// <summary>
        /// Gets the health status of the audit service
        /// </summary>
        /// <returns>Health status information</returns>
        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                var health = await _auditService.GetHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get audit service health");
                return StatusCode(500, new { Error = "Failed to get audit service health" });
            }
        }

        /// <summary>
        /// Archives audit events older than the specified date
        /// </summary>
        /// <param name="cutoffDate">Events older than this date will be archived</param>
        /// <returns>Number of events archived</returns>
        [HttpPost("archive")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ArchiveEvents([FromBody] ArchiveEventsRequest request)
        {
            try
            {
                var archivedCount = await _auditService.ArchiveEventsAsync(request.CutoffDate);
                return Ok(new { ArchivedCount = archivedCount, Message = "Events archived successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive audit events");
                return StatusCode(500, new { Error = "Failed to archive audit events" });
            }
        }
    }

    /// <summary>
    /// Request model for logging user actions
    /// </summary>
    public class LogUserActionRequest
    {
        /// <summary>
        /// User ID performing the action
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of action performed
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Target resource or entity
        /// </summary>
        public string TargetResource { get; set; } = string.Empty;

        /// <summary>
        /// Status of the action
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request model for logging system events
    /// </summary>
    public class LogSystemEventRequest
    {
        /// <summary>
        /// Type of system action
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Target resource or entity
        /// </summary>
        public string TargetResource { get; set; } = string.Empty;

        /// <summary>
        /// Status of the action
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request model for logging security events
    /// </summary>
    public class LogSecurityEventRequest
    {
        /// <summary>
        /// User ID involved in the security event
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of security action
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Target resource or entity
        /// </summary>
        public string TargetResource { get; set; } = string.Empty;

        /// <summary>
        /// Status of the action
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Risk level assessment
        /// </summary>
        public string RiskLevel { get; set; } = string.Empty;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request model for archiving events
    /// </summary>
    public class ArchiveEventsRequest
    {
        /// <summary>
        /// Events older than this date will be archived
        /// </summary>
        public DateTime CutoffDate { get; set; }
    }
}
