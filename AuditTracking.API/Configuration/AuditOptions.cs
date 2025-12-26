namespace AuditTracking.API.Configuration;

/// <summary>
/// Configuration options for the audit tracking service.
/// </summary>
public class AuditOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether automatic audit logging is enabled.
    /// </summary>
    public bool EnableAutomaticLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of entity types to exclude from automatic auditing.
    /// </summary>
    public List<Type> ExcludedEntityTypes { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of property names to exclude from auditing.
    /// </summary>
    public List<string> ExcludedProperties { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether soft delete tracking is enabled.
    /// </summary>
    public bool TrackSoftDeletes { get; set; } = true;

    /// <summary>
    /// Gets or sets the property name used to identify soft deletes.
    /// </summary>
    public string SoftDeletePropertyName { get; set; } = "IsDeleted";

    /// <summary>
    /// Gets or sets a function to resolve the current user identifier.
    /// </summary>
    public Func<IServiceProvider, string>? UserIdResolver { get; set; }

    /// <summary>
    /// Gets or sets a function to resolve the current tenant identifier.
    /// </summary>
    public Func<IServiceProvider, string?>? TenantIdResolver { get; set; }
}
