namespace AuditTracking.API.Entities;

/// <summary>
/// Represents an audit log entry that tracks changes made to entities.
/// </summary>
public class AuditLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the audit log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the entity that was modified.
    /// </summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the entity that was modified.
    /// </summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the action performed (Create, Update, Delete, SoftDelete).
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the user who made the change.
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the change was made.
    /// </summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the old values before the change.
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// Gets or sets the JSON representation of the new values after the change.
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Gets or sets the optional tenant identifier for multi-tenant scenarios.
    /// </summary>
    public string? TenantId { get; set; }
}
