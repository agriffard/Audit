using System.Text.Json;
using AuditTracking.API.Configuration;
using AuditTracking.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuditTracking.API.Interceptors;

/// <summary>
/// EF Core interceptor that automatically captures entity changes and creates audit logs.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuditDbContext _auditDbContext;
    private List<AuditEntry>? _pendingAudits;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditSaveChangesInterceptor"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="auditDbContext">The audit database context.</param>
    public AuditSaveChangesInterceptor(IServiceProvider serviceProvider, AuditDbContext auditDbContext)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _auditDbContext = auditDbContext ?? throw new ArgumentNullException(nameof(auditDbContext));
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var options = _serviceProvider.GetService<IOptions<AuditOptions>>()?.Value ?? new AuditOptions();

        if (!options.EnableAutomaticLogging)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        _pendingAudits = CreateAuditEntries(eventData.Context, options);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_pendingAudits is null || _pendingAudits.Count == 0 || eventData.Context is null)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        await SaveAuditLogsAsync(eventData.Context, _pendingAudits, cancellationToken);
        _pendingAudits = null;

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is null)
        {
            return base.SavingChanges(eventData, result);
        }

        var options = _serviceProvider.GetService<IOptions<AuditOptions>>()?.Value ?? new AuditOptions();

        if (!options.EnableAutomaticLogging)
        {
            return base.SavingChanges(eventData, result);
        }

        _pendingAudits = CreateAuditEntries(eventData.Context, options);

        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        if (_pendingAudits is null || _pendingAudits.Count == 0 || eventData.Context is null)
        {
            return base.SavedChanges(eventData, result);
        }

        SaveAuditLogsAsync(eventData.Context, _pendingAudits, CancellationToken.None).GetAwaiter().GetResult();
        _pendingAudits = null;

        return base.SavedChanges(eventData, result);
    }

    private List<AuditEntry> CreateAuditEntries(DbContext context, AuditOptions options)
    {
        var entries = new List<AuditEntry>();
        var userId = options.UserIdResolver?.Invoke(_serviceProvider) ?? "System";
        var tenantId = options.TenantIdResolver?.Invoke(_serviceProvider);

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog ||
                entry.State == EntityState.Detached ||
                entry.State == EntityState.Unchanged)
            {
                continue;
            }

            var entityType = entry.Entity.GetType();

            if (options.ExcludedEntityTypes.Contains(entityType))
            {
                continue;
            }

            var auditEntry = new AuditEntry
            {
                EntityName = entityType.Name,
                EntityId = GetPrimaryKeyValue(entry),
                Action = GetAction(entry, options),
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow,
                TenantId = tenantId
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    auditEntry.NewValues = GetPropertyValues(entry, e => e.CurrentValues, options.ExcludedProperties);
                    break;
                case EntityState.Deleted:
                    auditEntry.OldValues = GetPropertyValues(entry, e => e.OriginalValues, options.ExcludedProperties);
                    break;
                case EntityState.Modified:
                    auditEntry.OldValues = GetChangedPropertyValues(entry, e => e.OriginalValues, options.ExcludedProperties);
                    auditEntry.NewValues = GetChangedPropertyValues(entry, e => e.CurrentValues, options.ExcludedProperties);
                    break;
            }

            entries.Add(auditEntry);
        }

        return entries;
    }

    private static string GetAction(EntityEntry entry, AuditOptions options)
    {
        if (entry.State == EntityState.Modified && options.TrackSoftDeletes)
        {
            var softDeleteProperty = entry.Properties
                .FirstOrDefault(p => p.Metadata.Name == options.SoftDeletePropertyName);

            if (softDeleteProperty != null &&
                softDeleteProperty.IsModified &&
                softDeleteProperty.CurrentValue is true)
            {
                return AuditActions.SoftDelete;
            }
        }

        return entry.State switch
        {
            EntityState.Added => AuditActions.Create,
            EntityState.Deleted => AuditActions.Delete,
            EntityState.Modified => AuditActions.Update,
            _ => string.Empty
        };
    }

    private static string GetPrimaryKeyValue(EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;

        if (keyProperties == null || keyProperties.Count == 0)
        {
            return string.Empty;
        }

        if (keyProperties.Count == 1)
        {
            var value = entry.Property(keyProperties[0].Name).CurrentValue;
            return value?.ToString() ?? string.Empty;
        }

        var compositeKey = keyProperties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => v != null);

        return string.Join("_", compositeKey);
    }

    private static string GetPropertyValues(
        EntityEntry entry,
        Func<EntityEntry, PropertyValues> valuesSelector,
        List<string> excludedProperties)
    {
        var values = new Dictionary<string, object?>();
        var propertyValues = valuesSelector(entry);

        foreach (var property in propertyValues.Properties)
        {
            if (excludedProperties.Contains(property.Name))
            {
                continue;
            }

            values[property.Name] = propertyValues[property];
        }

        return JsonSerializer.Serialize(values);
    }

    private static string GetChangedPropertyValues(
        EntityEntry entry,
        Func<EntityEntry, PropertyValues> valuesSelector,
        List<string> excludedProperties)
    {
        var values = new Dictionary<string, object?>();
        var propertyValues = valuesSelector(entry);

        foreach (var property in entry.Properties.Where(p => p.IsModified))
        {
            if (excludedProperties.Contains(property.Metadata.Name))
            {
                continue;
            }

            values[property.Metadata.Name] = propertyValues[property.Metadata];
        }

        return JsonSerializer.Serialize(values);
    }

    private async Task SaveAuditLogsAsync(
        DbContext context,
        List<AuditEntry> auditEntries,
        CancellationToken cancellationToken)
    {
        var auditLogs = auditEntries.Select(e => new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = e.EntityName,
            EntityId = e.EntityId,
            Action = e.Action,
            ChangedBy = e.ChangedBy,
            ChangedAt = e.ChangedAt,
            OldValues = e.OldValues,
            NewValues = e.NewValues,
            TenantId = e.TenantId
        }).ToList();

        await _auditDbContext.AuditLogs.AddRangeAsync(auditLogs, cancellationToken);
        await _auditDbContext.SaveChangesAsync(cancellationToken);
    }

    private class AuditEntry
    {
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? TenantId { get; set; }
    }
}
