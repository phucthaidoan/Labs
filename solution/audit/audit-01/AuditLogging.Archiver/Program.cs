using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using AuditLogging.Core.Configuration;
using AuditLogging.Core.Interfaces;
using AuditLogging.Infrastructure.Data;
using AuditLogging.Infrastructure.Sinks;
using AuditLogging.Infrastructure.Services;
using AuditLogging.Services;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Configure audit logging options
        services.Configure<AuditLoggingOptions>(
            context.Configuration.GetSection(AuditLoggingOptions.SectionName));

        var auditOptions = context.Configuration.GetSection(AuditLoggingOptions.SectionName)
            .Get<AuditLoggingOptions>() ?? new AuditLoggingOptions();

        // Add Entity Framework
        if (auditOptions.DatabaseSink.Enabled)
        {
            if (auditOptions.DatabaseSink.Provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<AuditLoggingDbContext>(options =>
                    options.UseSqlServer(auditOptions.DatabaseSink.ConnectionString,
                        sqlOptions => sqlOptions.CommandTimeout(auditOptions.DatabaseSink.CommandTimeout)));
            }
            /*else if (auditOptions.DatabaseSink.Provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                services.AddDbContext<AuditLoggingDbContext>(options =>
                    options.UseNpgsql(auditOptions.DatabaseSink.ConnectionString,
                        npgsqlOptions => npgsqlOptions.CommandTimeout(auditOptions.DatabaseSink.CommandTimeout)));
            }*/
            else
            {
                services.AddDbContext<AuditLoggingDbContext>(options =>
                    options.UseInMemoryDatabase("AuditLoggingDb"));
            }
        }

        // Add Azure Blob Storage client
        if (auditOptions.BlobStorageSink.Enabled)
        {
            services.AddSingleton<BlobServiceClient>(provider =>
            {
                var connectionString = auditOptions.BlobStorageSink.ConnectionString;
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = "UseDevelopmentStorage=true";
                }
                return new BlobServiceClient(connectionString);
            });
        }

        // Add audit logging services
        services.AddScoped<IDataProtectionService, DataProtectionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IExportService, ExportService>();

        // Add audit sinks
        if (auditOptions.DatabaseSink.Enabled)
        {
            services.AddScoped<IAuditSink, DatabaseSink>();
        }

        if (auditOptions.BlobStorageSink.Enabled)
        {
            services.AddScoped<IAuditSink, BlobStorageSink>();
        }

        // Add Quartz scheduler
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();

            // Archive job
            var archiveJobKey = new JobKey("ArchiveJob");
            q.AddJob<ArchiveJob>(opts => opts.WithIdentity(archiveJobKey));

            q.AddTrigger(opts => opts
                .ForJob(archiveJobKey)
                .WithIdentity("ArchiveTrigger")
                .WithCronSchedule(auditOptions.ArchivalJob.Schedule));

            // Cleanup job
            var cleanupJobKey = new JobKey("CleanupJob");
            q.AddJob<CleanupJob>(opts => opts.WithIdentity(cleanupJobKey));

            q.AddTrigger(opts => opts
                .ForJob(cleanupJobKey)
                .WithIdentity("CleanupTrigger")
                .WithCronSchedule(auditOptions.ArchivalJob.Schedule));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
    })
    .Build();

// Initialize database
using var scope = host.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AuditLoggingDbContext>();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    await context.Database.EnsureCreatedAsync();
    await context.SeedDataAsync();
    logger.LogInformation("Database initialized successfully");
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to initialize database");
}

logger.LogInformation("Audit Logging Archiver started successfully");

await host.RunAsync();

// Archive job implementation
public class ArchiveJob : IJob
{
    private readonly IAuditService _auditService;
    private readonly ILogger<ArchiveJob> _logger;
    private readonly AuditLoggingOptions _options;

    public ArchiveJob(IAuditService auditService, ILogger<ArchiveJob> logger, IOptions<AuditLoggingOptions> options)
    {
        _auditService = auditService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting archival job");

            var cutoffDate = DateTime.UtcNow.Subtract(_options.RetentionPolicies.OperationalRetentionPeriod);
            var archivedCount = await _auditService.ArchiveEventsAsync(cutoffDate);

            _logger.LogInformation("Archival job completed. Archived {Count} events", archivedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Archival job failed");
        }
    }
}

// Cleanup job implementation
public class CleanupJob : IJob
{
    private readonly IAuditService _auditService;
    private readonly ILogger<CleanupJob> _logger;
    private readonly AuditLoggingOptions _options;

    public CleanupJob(IAuditService auditService, ILogger<CleanupJob> logger, IOptions<AuditLoggingOptions> options)
    {
        _auditService = auditService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            _logger.LogInformation("Starting cleanup job");

            // This would typically clean up old export files, temporary data, etc.
            // For now, we'll just log the job execution
            _logger.LogInformation("Cleanup job completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup job failed");
        }
    }
}
