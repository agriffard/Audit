using AuditTracking.API.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AuditTracking.API.Extensions;

/// <summary>
/// Extension methods for DbContextOptionsBuilder.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds the audit interceptor to the DbContext options.
    /// </summary>
    /// <param name="optionsBuilder">The DbContext options builder.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The DbContext options builder for chaining.</returns>
    public static DbContextOptionsBuilder AddAuditInterceptor(
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var interceptor = serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
        optionsBuilder.AddInterceptors(interceptor);

        return optionsBuilder;
    }
}
