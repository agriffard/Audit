using AuditTracking.API.Entities;

namespace AuditTracking.API.Services;

/// <summary>
/// Service interface for audit logging operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Logs an audit entry for an entity.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="entity">The entity being audited.</param>
    /// <param name="action">The action performed (Create, Update, Delete, SoftDelete).</param>
    /// <param name="userId">The identifier of the user performing the action.</param>
    /// <param name="oldValues">The optional JSON representation of old values.</param>
    /// <param name="newValues">The optional JSON representation of new values.</param>
    /// <param name="tenantId">The optional tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogAsync<T>(
        T entity,
        string action,
        string userId,
        string? oldValues = null,
        string? newValues = null,
        string? tenantId = null,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets audit logs for a specific entity.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity filtered by tenant.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        string tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit logs for a specific entity within a date range.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="entityId">The identifier of the entity.</param>
    /// <param name="from">The start date of the range.</param>
    /// <param name="to">The end date of the range.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
        string entityName,
        string entityId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all audit logs for an entity type.
    /// </summary>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A collection of audit log entries.</returns>
    Task<IEnumerable<AuditLog>> GetAuditLogsByEntityNameAsync(
        string entityName,
        CancellationToken cancellationToken = default);
}
