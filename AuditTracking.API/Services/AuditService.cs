using System.Text.Json;
using AuditTracking.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditTracking.API.Services;

/// <summary>
/// Default implementation of the audit service.
/// </summary>
public class AuditService : IAuditService
{
    private readonly AuditDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditService"/> class.
    /// </summary>
    /// <param name="context">The audit database context.</param>
    public AuditService(AuditDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task LogAsync<T>(
        T entity,
        string action,
        string userId,
        string? oldValues = null,
        string? newValues = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentException.ThrowIfNullOrEmpty(action);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var entityType = typeof(T);
        var entityId = GetEntityId(entity);

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityType.Name,
            EntityId = entityId,
            Action = action,
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow,
            OldValues = oldValues,
            NewValues = newValues ?? SerializeEntity(entity),
            TenantId = tenantId
        };

        await _context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityName);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityName);
        ArgumentException.ThrowIfNullOrEmpty(entityId);
        ArgumentException.ThrowIfNullOrEmpty(tenantId);

        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName &&
                       a.EntityId == entityId &&
                       a.TenantId == tenantId)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityName);
        ArgumentException.ThrowIfNullOrEmpty(entityId);

        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName &&
                       a.EntityId == entityId &&
                       a.ChangedAt >= from &&
                       a.ChangedAt <= to)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditLogsByEntityNameAsync(
        string entityName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(entityName);

        return await _context.AuditLogs
            .Where(a => a.EntityName == entityName)
            .OrderByDescending(a => a.ChangedAt)
            .ToListAsync(cancellationToken);
    }

    private static string GetEntityId<T>(T entity) where T : class
    {
        // Try to find an Id property using common naming conventions
        var type = typeof(T);
        var idProperty = type.GetProperty("Id") ??
                        type.GetProperty($"{type.Name}Id") ??
                        type.GetProperty("ID");

        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            return value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string SerializeEntity<T>(T entity) where T : class
    {
        return JsonSerializer.Serialize(entity, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
