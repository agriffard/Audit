using AuditTracking.API;
using AuditTracking.API.Extensions;
using AuditTracking.API.Interceptors;
using AuditTracking.API.Services;
using AuditTracking.Example.Data;
using AuditTracking.Example.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add AuditTracking services with InMemory database for demo purposes
builder.Services.AddAuditTracking(
    options => options.UseInMemoryDatabase("AuditDb"),
    options =>
    {
        options.EnableAutomaticLogging = true;
        options.TrackSoftDeletes = true;
        options.SoftDeletePropertyName = "IsDeleted";
        options.UserIdResolver = sp =>
        {
            // In a real app, this would come from HttpContext.User
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "anonymous";
        };
    });

// Add the application's DbContext with audit interceptor
builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    options.UseInMemoryDatabase("AppDb");
    options.AddAuditInterceptor(sp);
});

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Product endpoints
app.MapGet("/products", async (ApplicationDbContext db) =>
{
    var products = await db.Products.Where(p => !p.IsDeleted).ToListAsync();
    return Results.Ok(products);
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/products/{id}", async (int id, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    return product is null ? Results.NotFound() : Results.Ok(product);
})
.WithName("GetProduct")
.WithOpenApi();

app.MapPost("/products", async (Product product, ApplicationDbContext db) =>
{
    product.CreatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

app.MapPut("/products/{id}", async (int id, Product input, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    product.Name = input.Name;
    product.Description = input.Description;
    product.Price = input.Price;
    product.StockQuantity = input.StockQuantity;
    product.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(product);
})
.WithName("UpdateProduct")
.WithOpenApi();

app.MapDelete("/products/{id}", async (int id, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null)
    {
        return Results.NotFound();
    }

    // Soft delete
    product.IsDeleted = true;
    product.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();

// Audit endpoints
app.MapGet("/audit/{entityName}/{entityId}", async (string entityName, string entityId, IAuditService auditService) =>
{
    var logs = await auditService.GetAuditLogsAsync(entityName, entityId);
    return Results.Ok(logs);
})
.WithName("GetAuditLogs")
.WithOpenApi();

app.MapGet("/audit/{entityName}", async (string entityName, IAuditService auditService) =>
{
    var logs = await auditService.GetAuditLogsByEntityNameAsync(entityName);
    return Results.Ok(logs);
})
.WithName("GetAuditLogsByEntity")
.WithOpenApi();

// Manual audit logging endpoint (for demonstration)
app.MapPost("/audit/log", async (AuditLogRequest request, IAuditService auditService, ApplicationDbContext db) =>
{
    var product = await db.Products.FindAsync(request.EntityId);
    if (product is null)
    {
        return Results.NotFound();
    }

    await auditService.LogAsync(product, request.Action, request.UserId);
    return Results.Ok();
})
.WithName("LogAudit")
.WithOpenApi();

app.Run();

/// <summary>
/// Request model for manual audit logging.
/// </summary>
/// <param name="EntityId">The entity identifier.</param>
/// <param name="Action">The action performed.</param>
/// <param name="UserId">The user identifier.</param>
public record AuditLogRequest(int EntityId, string Action, string UserId);
