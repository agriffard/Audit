# Audit Tracking API

A centralized mechanism for tracking and historizing all EF Core entity modifications, enabling audit, compliance, and change analysis.

## Features

- **Automatic Change Tracking**: Intercepts `SaveChangesAsync` to automatically capture entity changes
- **Manual Logging**: Service API for custom audit logging
- **Multi-tenant Support**: Optional tenant filtering for multi-tenant applications
- **Soft Delete Detection**: Automatically detects and tracks soft deletes
- **Flexible Configuration**: Exclude specific entities or properties from auditing
- **Query API**: Retrieve audit logs by entity, date range, or tenant

## Installation

Add the NuGet package to your project:

```bash
dotnet add package AuditTracking.API
```

## Quick Start

### 1. Configure Services

```csharp
using AuditTracking.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add audit tracking with SQL Server
builder.Services.AddAuditTracking(
    connectionString: "Your-Connection-String",
    options =>
    {
        options.EnableAutomaticLogging = true;
        options.TrackSoftDeletes = true;
        options.SoftDeletePropertyName = "IsDeleted";
        options.UserIdResolver = sp =>
        {
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "system";
        };
    });

// Add your application DbContext with the audit interceptor
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseSqlServer("Your-Connection-String");
    options.AddAuditInterceptor(sp);
});
```

### 2. Use the Audit Service

```csharp
using AuditTracking.API.Services;

public class ProductService
{
    private readonly IAuditService _auditService;

    public ProductService(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // Manual logging
    public async Task LogCustomAction(Product product, string userId)
    {
        await _auditService.LogAsync(product, "CustomAction", userId);
    }

    // Retrieve audit logs
    public async Task<IEnumerable<AuditLog>> GetProductHistory(string productId)
    {
        return await _auditService.GetAuditLogsAsync("Product", productId);
    }
}
```

### 3. Query Audit Logs

```csharp
// Get all logs for a specific entity
var logs = await auditService.GetAuditLogsAsync("Product", "123");

// Get logs with tenant filtering
var tenantLogs = await auditService.GetAuditLogsAsync("Product", "123", "tenant-1");

// Get logs within a date range
var dateRangeLogs = await auditService.GetAuditLogsAsync(
    "Product", 
    "123", 
    DateTime.UtcNow.AddDays(-7), 
    DateTime.UtcNow);

// Get all logs for an entity type
var allProductLogs = await auditService.GetAuditLogsByEntityNameAsync("Product");
```

## Audit Log Entity

```csharp
public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityName { get; set; }
    public string EntityId { get; set; }
    public string Action { get; set; }      // Create, Update, Delete, SoftDelete
    public string ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? OldValues { get; set; }  // JSON
    public string? NewValues { get; set; }  // JSON
    public string? TenantId { get; set; }   // Optional
}
```

## Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `EnableAutomaticLogging` | Enable/disable automatic interception | `true` |
| `ExcludedEntityTypes` | List of entity types to exclude from auditing | Empty |
| `ExcludedProperties` | List of property names to exclude | Empty |
| `TrackSoftDeletes` | Track soft delete operations | `true` |
| `SoftDeletePropertyName` | Property name for soft delete detection | `"IsDeleted"` |
| `UserIdResolver` | Function to resolve current user ID | `null` |
| `TenantIdResolver` | Function to resolve current tenant ID | `null` |

## Database Schema

The library creates an `AuditLogs` table with the following indexes for optimal query performance:

- `IX_AuditLogs_EntityName`
- `IX_AuditLogs_EntityId`
- `IX_AuditLogs_ChangedAt`
- `IX_AuditLogs_EntityName_EntityId`
- `IX_AuditLogs_TenantId`

## Example Application

See the `AuditTracking.Example` project for a complete ASP.NET Core Web API example with:

- Product CRUD endpoints with automatic audit tracking
- Soft delete support
- Audit log query endpoints
- Swagger/OpenAPI documentation

## Running the Example

```bash
cd AuditTracking.Example
dotnet run
```

Navigate to `https://localhost:5001/swagger` to explore the API.

## Project Structure

```
├── AuditTracking.API/           # Core NuGet library
│   ├── Configuration/           # Audit options
│   ├── Entities/                # AuditLog entity
│   ├── Extensions/              # DI extension methods
│   ├── Interceptors/            # SaveChanges interceptor
│   └── Services/                # Audit service interface and implementation
├── AuditTracking.API.Tests/     # Unit tests
└── AuditTracking.Example/       # ASP.NET Core example application
```

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0 or later
- SQL Server (or any EF Core compatible database)

## License

MIT