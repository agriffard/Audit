using AuditTracking.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditTracking.API;

/// <summary>
/// DbContext for audit logging functionality.
/// </summary>
public class AuditDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the audit logs.
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasDefaultValueSql("NEWID()");

            entity.Property(e => e.EntityName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.EntityId)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.ChangedBy)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ChangedAt)
                .IsRequired();

            entity.Property(e => e.TenantId)
                .HasMaxLength(256);

            // Create indexes for common query patterns
            entity.HasIndex(e => e.EntityName)
                .HasDatabaseName("IX_AuditLogs_EntityName");

            entity.HasIndex(e => e.EntityId)
                .HasDatabaseName("IX_AuditLogs_EntityId");

            entity.HasIndex(e => e.ChangedAt)
                .HasDatabaseName("IX_AuditLogs_ChangedAt");

            entity.HasIndex(e => new { e.EntityName, e.EntityId })
                .HasDatabaseName("IX_AuditLogs_EntityName_EntityId");

            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("IX_AuditLogs_TenantId");
        });
    }
}
