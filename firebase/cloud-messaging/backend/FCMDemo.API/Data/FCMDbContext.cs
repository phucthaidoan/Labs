using Microsoft.EntityFrameworkCore;
using FCMDemo.API.Data.Entities;

namespace FCMDemo.API.Data;

/// <summary>
/// Entity Framework DbContext for FCM Demo application
/// </summary>
public class FCMDbContext : DbContext
{
    public FCMDbContext(DbContextOptions<FCMDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Device tokens collection
    /// </summary>
    public DbSet<DeviceToken> DeviceTokens { get; set; }

    /// <summary>
    /// Scheduled notifications collection
    /// </summary>
    public DbSet<ScheduledNotification> ScheduledNotifications { get; set; }

    /// <summary>
    /// Notification logs collection (audit trail)
    /// </summary>
    public DbSet<NotificationLog> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DeviceToken entity
        modelBuilder.Entity<DeviceToken>(entity =>
        {
            // Unique index on DeviceId to prevent duplicate devices
            entity.HasIndex(e => e.DeviceId)
                .IsUnique();

            // Index on UserId for faster user token lookups
            entity.HasIndex(e => e.UserId);

            // Composite index for active user tokens
            entity.HasIndex(e => new { e.UserId, e.IsActive });
        });

        // Configure ScheduledNotification entity
        modelBuilder.Entity<ScheduledNotification>(entity =>
        {
            // Index on ScheduledFor for efficient background job queries
            entity.HasIndex(e => e.ScheduledFor);

            // Index on Status for filtering pending/sent notifications
            entity.HasIndex(e => e.Status);

            // Composite index for finding due notifications (most common query)
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });

            // Index on UserId for user-specific queries
            entity.HasIndex(e => e.UserId);

            // Configure relationship with NotificationLog
            entity.HasOne(s => s.NotificationLog)
                .WithOne(l => l.ScheduledNotification)
                .HasForeignKey<ScheduledNotification>(s => s.NotificationLogId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure NotificationLog entity
        modelBuilder.Entity<NotificationLog>(entity =>
        {
            // Index on UserId for user history queries
            entity.HasIndex(e => e.UserId);

            // Index on SentAt for chronological queries
            entity.HasIndex(e => e.SentAt);

            // Composite index for user history with time-based filtering
            entity.HasIndex(e => new { e.UserId, e.SentAt });

            // Index on Success for filtering successful/failed notifications
            entity.HasIndex(e => e.Success);
        });
    }
}
