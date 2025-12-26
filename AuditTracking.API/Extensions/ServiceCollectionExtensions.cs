using AuditTracking.API.Configuration;
using AuditTracking.API.Interceptors;
using AuditTracking.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditTracking.API.Extensions;

/// <summary>
/// Extension methods for registering audit tracking services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds audit tracking services to the service collection using SQL Server.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <param name="configureOptions">Optional configuration for audit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditTracking(
        this IServiceCollection services,
        string connectionString,
        Action<AuditOptions>? configureOptions = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString);

        var options = new AuditOptions();
        configureOptions?.Invoke(options);
        services.Configure<AuditOptions>(opts =>
        {
            opts.EnableAutomaticLogging = options.EnableAutomaticLogging;
            opts.ExcludedEntityTypes = options.ExcludedEntityTypes;
            opts.ExcludedProperties = options.ExcludedProperties;
            opts.TrackSoftDeletes = options.TrackSoftDeletes;
            opts.SoftDeletePropertyName = options.SoftDeletePropertyName;
            opts.UserIdResolver = options.UserIdResolver;
            opts.TenantIdResolver = options.TenantIdResolver;
        });

        services.AddDbContext<AuditDbContext>(dbOptions =>
            dbOptions.UseSqlServer(connectionString));

        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }

    /// <summary>
    /// Adds audit tracking services to the service collection with custom DbContext configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Configuration for the DbContext.</param>
    /// <param name="configureOptions">Optional configuration for audit options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuditTracking(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<AuditOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var options = new AuditOptions();
        configureOptions?.Invoke(options);
        services.Configure<AuditOptions>(opts =>
        {
            opts.EnableAutomaticLogging = options.EnableAutomaticLogging;
            opts.ExcludedEntityTypes = options.ExcludedEntityTypes;
            opts.ExcludedProperties = options.ExcludedProperties;
            opts.TrackSoftDeletes = options.TrackSoftDeletes;
            opts.SoftDeletePropertyName = options.SoftDeletePropertyName;
            opts.UserIdResolver = options.UserIdResolver;
            opts.TenantIdResolver = options.TenantIdResolver;
        });

        services.AddDbContext<AuditDbContext>(configureDbContext);
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        return services;
    }
}
