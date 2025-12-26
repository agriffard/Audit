using AuditTracking.API.Configuration;
using AuditTracking.API.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AuditTracking.API.Tests;

public class AuditSaveChangesInterceptorTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AuditDbContext _auditContext;
    private readonly TestDbContext _testContext;
    private readonly string _auditDbName;
    private readonly string _testDbName;

    public AuditSaveChangesInterceptorTests()
    {
        _auditDbName = $"AuditDb_{Guid.NewGuid()}";
        _testDbName = $"TestDb_{Guid.NewGuid()}";
        
        var services = new ServiceCollection();

        // Configure audit options
        services.Configure<AuditOptions>(options =>
        {
            options.EnableAutomaticLogging = true;
            options.UserIdResolver = _ => "testUser";
        });

        // Add audit context
        services.AddDbContext<AuditDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: _auditDbName));

        // Add interceptor
        services.AddScoped<AuditSaveChangesInterceptor>();

        _serviceProvider = services.BuildServiceProvider();

        // Create contexts
        _auditContext = _serviceProvider.GetRequiredService<AuditDbContext>();

        var interceptor = _serviceProvider.GetRequiredService<AuditSaveChangesInterceptor>();
        var testOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: _testDbName)
            .AddInterceptors(interceptor)
            .Options;

        _testContext = new TestDbContext(testOptions);
    }

    public void Dispose()
    {
        _testContext.Dispose();
        _auditContext.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityAdded()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Price = 9.99m };

        // Act
        _testContext.Products.Add(product);
        await _testContext.SaveChangesAsync();

        // Assert
        var logs = await _auditContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Product", logs[0].EntityName);
        Assert.Equal(AuditActions.Create, logs[0].Action);
        Assert.Equal("testUser", logs[0].ChangedBy);
        Assert.NotNull(logs[0].NewValues);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityModified()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Price = 9.99m };
        _testContext.Products.Add(product);
        await _testContext.SaveChangesAsync();
        _auditContext.AuditLogs.RemoveRange(_auditContext.AuditLogs); // Clear initial logs
        await _auditContext.SaveChangesAsync();

        // Act
        product.Price = 19.99m;
        await _testContext.SaveChangesAsync();

        // Assert
        var logs = await _auditContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Product", logs[0].EntityName);
        Assert.Equal(AuditActions.Update, logs[0].Action);
        Assert.NotNull(logs[0].OldValues);
        Assert.NotNull(logs[0].NewValues);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateAuditLog_WhenEntityDeleted()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Price = 9.99m };
        _testContext.Products.Add(product);
        await _testContext.SaveChangesAsync();
        _auditContext.AuditLogs.RemoveRange(_auditContext.AuditLogs); // Clear initial logs
        await _auditContext.SaveChangesAsync();

        // Act
        _testContext.Products.Remove(product);
        await _testContext.SaveChangesAsync();

        // Assert
        var logs = await _auditContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("Product", logs[0].EntityName);
        Assert.Equal(AuditActions.Delete, logs[0].Action);
        Assert.NotNull(logs[0].OldValues);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCreateSoftDeleteAuditLog_WhenSoftDeleteDetected()
    {
        // Arrange
        var softDeleteProduct = new SoftDeleteProduct { Name = "Test Product", IsDeleted = false };
        _testContext.SoftDeleteProducts.Add(softDeleteProduct);
        await _testContext.SaveChangesAsync();
        _auditContext.AuditLogs.RemoveRange(_auditContext.AuditLogs); // Clear initial logs
        await _auditContext.SaveChangesAsync();

        // Act
        softDeleteProduct.IsDeleted = true;
        await _testContext.SaveChangesAsync();

        // Assert
        var logs = await _auditContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(AuditActions.SoftDelete, logs[0].Action);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotCreateAuditLog_WhenDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<AuditOptions>(options =>
        {
            options.EnableAutomaticLogging = false;
        });
        services.AddDbContext<AuditDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: $"DisabledAuditDb_{Guid.NewGuid()}"));
        services.AddScoped<AuditSaveChangesInterceptor>();

        using var sp = services.BuildServiceProvider();
        var auditContext = sp.GetRequiredService<AuditDbContext>();
        var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();

        var testOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"DisabledTestDb_{Guid.NewGuid()}")
            .AddInterceptors(interceptor)
            .Options;

        using var testContext = new TestDbContext(testOptions);

        // Act
        testContext.Products.Add(new Product { Name = "Test", Price = 1 });
        await testContext.SaveChangesAsync();

        // Assert
        var logs = await auditContext.AuditLogs.ToListAsync();
        Assert.Empty(logs);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldCapturePrimaryKeyValue()
    {
        // Arrange
        var product = new Product { Name = "Test Product", Price = 9.99m };

        // Act
        _testContext.Products.Add(product);
        await _testContext.SaveChangesAsync();

        // Assert
        var logs = await _auditContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(product.Id.ToString(), logs[0].EntityId);
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<SoftDeleteProduct> SoftDeleteProducts { get; set; } = null!;
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    public class SoftDeleteProduct
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
