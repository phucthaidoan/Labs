using Microsoft.EntityFrameworkCore;
using AuditLogging.Core.Models;
using AuditLogging.Core.Configuration;
using AuditLogging.Core.Interfaces;

namespace AuditLogging.Infrastructure.Data
{
    /// <summary>
    /// Entity Framework DbContext for audit logging
    /// </summary>
    public class AuditLoggingDbContext : DbContext
    {
        private readonly AuditLoggingOptions _options;

        public AuditLoggingDbContext(DbContextOptions<AuditLoggingDbContext> options, AuditLoggingOptions auditOptions) 
            : base(options)
        {
            _options = auditOptions;
        }

        /// <summary>
        /// Audit events table
        /// </summary>
        public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

        /// <summary>
        /// Pseudonymization mappings table
        /// </summary>
        public DbSet<PseudonymizationMapping> PseudonymizationMappings { get; set; } = null!;

        /// <summary>
        /// Export jobs table
        /// </summary>
        public DbSet<ExportStatus> ExportJobs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure schema
            var schema = _options.DatabaseSink.SchemaName;
            if (!string.IsNullOrEmpty(schema))
            {
                modelBuilder.HasDefaultSchema(schema);
            }

            // Configure AuditEvents table
            var auditEventsTable = modelBuilder.Entity<AuditEvent>(entity =>
            {
                entity.ToTable(_options.DatabaseSink.TableName);
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever(); // We generate GUIDs ourselves
                
                // Configure required fields
                entity.Property(e => e.UserId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.ActionType).IsRequired().HasMaxLength(255);
                entity.Property(e => e.TargetResource).IsRequired().HasMaxLength(500);
                entity.Property(e => e.IP).HasMaxLength(45); // IPv6 support
                entity.Property(e => e.SessionId).HasMaxLength(255);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CorrelationId).HasMaxLength(255);
                entity.Property(e => e.UserAgent).HasMaxLength(1000);
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.RiskLevel).HasMaxLength(50);
                entity.Property(e => e.DataHash).HasMaxLength(255);

                // Configure metadata as JSON
                entity.Property(e => e.Metadata)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .HasColumnType("nvarchar(max)");

                // Configure timestamp with precision
                entity.Property(e => e.Timestamp)
                    .HasColumnType("datetime2(7)");

                // Configure retention category
                entity.Property(e => e.RetentionCategory)
                    .HasConversion<int>();

                // Add indexes for performance
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditEvents_Timestamp");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_AuditEvents_UserId");
                entity.HasIndex(e => e.ActionType).HasDatabaseName("IX_AuditEvents_ActionType");
                entity.HasIndex(e => e.TargetResource).HasDatabaseName("IX_AuditEvents_TargetResource");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_AuditEvents_Status");
                entity.HasIndex(e => e.CorrelationId).HasDatabaseName("IX_AuditEvents_CorrelationId");
                entity.HasIndex(e => e.RetentionCategory).HasDatabaseName("IX_AuditEvents_RetentionCategory");
                entity.HasIndex(e => new { e.Timestamp, e.RetentionCategory }).HasDatabaseName("IX_AuditEvents_Timestamp_RetentionCategory");
                entity.HasIndex(e => new { e.UserId, e.Timestamp }).HasDatabaseName("IX_AuditEvents_UserId_Timestamp");
                entity.HasIndex(e => new { e.ActionType, e.Timestamp }).HasDatabaseName("IX_AuditEvents_ActionType_Timestamp");

                // Configure table partitioning by retention category (if supported)
                if (_options.DatabaseSink.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                {
                    entity.HasIndex(e => e.RetentionCategory)
                        .HasDatabaseName("IX_AuditEvents_RetentionCategory_Partitioned")
                        .IsClustered(false);
                }
            });

            // Configure PseudonymizationMappings table
            var pseudonymizationTable = modelBuilder.Entity<PseudonymizationMapping>(entity =>
            {
                entity.ToTable("PseudonymizationMappings");
                entity.HasKey(e => e.PseudonymizedValue);
                
                entity.Property(e => e.OriginalValue).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.PseudonymizedValue).IsRequired().HasMaxLength(1000);
                entity.Property(e => e.FieldName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Method).IsRequired().HasMaxLength(100);
                
                // Configure context as JSON
                entity.Property(e => e.Context)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
                    .HasColumnType("nvarchar(max)");

                // Add indexes
                entity.HasIndex(e => e.OriginalValue).HasDatabaseName("IX_PseudonymizationMappings_OriginalValue");
                entity.HasIndex(e => e.FieldName).HasDatabaseName("IX_PseudonymizationMappings_FieldName");
                entity.HasIndex(e => e.PseudonymizedAt).HasDatabaseName("IX_PseudonymizationMappings_PseudonymizedAt");
                entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_PseudonymizationMappings_ExpiresAt");
            });

            // Configure ExportJobs table
            var exportJobsTable = modelBuilder.Entity<ExportStatus>(entity =>
            {
                entity.ToTable("ExportJobs");
                entity.HasKey(e => e.ExportId);
                
                entity.Property(e => e.ExportId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CurrentStage).HasMaxLength(255);
                entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
                
                // Configure status enum
                entity.Property(e => e.Status)
                    .HasConversion<int>();

                // Configure request as JSON
                entity.Property(e => e.Request)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<ExportRequest>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new ExportRequest())
                    .HasColumnType("nvarchar(max)");

                // Add indexes
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_ExportJobs_Status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("IX_ExportJobs_CreatedAt");
                entity.HasIndex(e => e.CompletedAt).HasDatabaseName("IX_ExportJobs_CompletedAt");
            });

            // Configure table compression and partitioning for large tables
            if (_options.DatabaseSink.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                // Enable data compression for audit events table
                modelBuilder.Entity<AuditEvent>()
                    .HasIndex(e => e.Timestamp)
                    .HasDatabaseName("IX_AuditEvents_Timestamp_Compressed")
                    .IsClustered()
                    .HasFilter("[RetentionCategory] = 0"); // Only operational logs

                // Configure table partitioning for archival logs
                if (_options.RetentionPolicies.AutoArchiveLogs)
                {
                    // This would require additional SQL scripts for table partitioning
                    // For now, we'll use filtered indexes for performance
                    modelBuilder.Entity<AuditEvent>()
                        .HasIndex(e => e.Timestamp)
                        .HasDatabaseName("IX_AuditEvents_Timestamp_Archival")
                        .HasFilter("[RetentionCategory] = 1"); // Only archival logs
                }
            }
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Configure JSON serialization for all properties of type Dictionary<string, object>
            configurationBuilder.Properties<Dictionary<string, object>>()
                .HaveColumnType("nvarchar(max)");
        }

        /// <summary>
        /// Seeds initial data if the database is empty
        /// </summary>
        public async Task SeedDataAsync()
        {
            if (!AuditEvents.Any())
            {
                // Add a system startup audit event
                var startupEvent = new AuditEvent
                {
                    UserId = "SYSTEM",
                    ActionType = "SystemStartup",
                    TargetResource = "AuditLoggingService",
                    IP = "127.0.0.1",
                    SessionId = "SYSTEM",
                    Status = "Success",
                    Metadata = new Dictionary<string, object>
                    {
                        { "Version", "1.0.0" },
                        { "StartupTime", DateTime.UtcNow },
                        { "Environment", _options.Environment }
                    },
                    RiskLevel = "Low",
                    ContainsSensitiveData = false,
                    RetentionCategory = RetentionCategory.Operational
                };

                AuditEvents.Add(startupEvent);
                await SaveChangesAsync();
            }
        }
    }
}
