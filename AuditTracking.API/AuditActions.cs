namespace AuditTracking.API;

/// <summary>
/// Constants for audit action types.
/// </summary>
public static class AuditActions
{
    /// <summary>
    /// Entity was created.
    /// </summary>
    public const string Create = "Create";

    /// <summary>
    /// Entity was updated.
    /// </summary>
    public const string Update = "Update";

    /// <summary>
    /// Entity was deleted.
    /// </summary>
    public const string Delete = "Delete";

    /// <summary>
    /// Entity was soft deleted.
    /// </summary>
    public const string SoftDelete = "SoftDelete";
}
