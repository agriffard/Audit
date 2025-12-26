using AuditTracking.API.Entities;
using AuditTracking.API.Services;
using Microsoft.EntityFrameworkCore;

namespace AuditTracking.API.Tests;

public class AuditServiceTests : IDisposable
{
    private readonly AuditDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuditDbContext(options);
        _service = new AuditService(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task LogAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        await _service.LogAsync(entity, AuditActions.Create, "user123");

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("TestEntity", logs[0].EntityName);
        Assert.Equal("1", logs[0].EntityId);
        Assert.Equal(AuditActions.Create, logs[0].Action);
        Assert.Equal("user123", logs[0].ChangedBy);
    }

    [Fact]
    public async Task LogAsync_ShouldIncludeTenantId_WhenProvided()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act
        await _service.LogAsync(entity, AuditActions.Create, "user123", tenantId: "tenant1");

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("tenant1", logs[0].TenantId);
    }

    [Fact]
    public async Task LogAsync_ShouldIncludeOldAndNewValues_WhenProvided()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Updated" };
        var oldValues = "{\"Name\":\"Original\"}";
        var newValues = "{\"Name\":\"Updated\"}";

        // Act
        await _service.LogAsync(entity, AuditActions.Update, "user123", oldValues, newValues);

        // Assert
        var logs = await _context.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal(oldValues, logs[0].OldValues);
        Assert.Equal(newValues, logs[0].NewValues);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldReturnLogsForEntity()
    {
        // Arrange
        await _context.AuditLogs.AddRangeAsync(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = DateTime.UtcNow.AddMinutes(-2) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Update, ChangedBy = "user2", ChangedAt = DateTime.UtcNow.AddMinutes(-1) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "2", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetAuditLogsAsync("TestEntity", "1");

        // Assert
        Assert.Equal(2, logs.Count());
    }

    [Fact]
    public async Task GetAuditLogsAsync_ShouldReturnLogsOrderedByDateDescending()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddMinutes(-2);
        var newDate = DateTime.UtcNow.AddMinutes(-1);

        await _context.AuditLogs.AddRangeAsync(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = oldDate },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Update, ChangedBy = "user2", ChangedAt = newDate }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = (await _service.GetAuditLogsAsync("TestEntity", "1")).ToList();

        // Assert
        Assert.Equal(AuditActions.Update, logs[0].Action);
        Assert.Equal(AuditActions.Create, logs[1].Action);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithTenant_ShouldFilterByTenant()
    {
        // Arrange
        await _context.AuditLogs.AddRangeAsync(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = DateTime.UtcNow, TenantId = "tenant1" },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Update, ChangedBy = "user2", ChangedAt = DateTime.UtcNow, TenantId = "tenant2" }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetAuditLogsAsync("TestEntity", "1", "tenant1");

        // Assert
        Assert.Single(logs);
        Assert.Equal("tenant1", logs.First().TenantId);
    }

    [Fact]
    public async Task GetAuditLogsAsync_WithDateRange_ShouldFilterByDates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _context.AuditLogs.AddRangeAsync(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = now.AddDays(-5) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Update, ChangedBy = "user2", ChangedAt = now.AddDays(-2) },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Update, ChangedBy = "user3", ChangedAt = now }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetAuditLogsAsync("TestEntity", "1", now.AddDays(-3), now.AddDays(-1));

        // Assert
        Assert.Single(logs);
    }

    [Fact]
    public async Task GetAuditLogsByEntityNameAsync_ShouldReturnAllLogsForEntityType()
    {
        // Arrange
        await _context.AuditLogs.AddRangeAsync(
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "TestEntity", EntityId = "2", Action = AuditActions.Create, ChangedBy = "user2", ChangedAt = DateTime.UtcNow },
            new AuditLog { Id = Guid.NewGuid(), EntityName = "OtherEntity", EntityId = "1", Action = AuditActions.Create, ChangedBy = "user1", ChangedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var logs = await _service.GetAuditLogsByEntityNameAsync("TestEntity");

        // Assert
        Assert.Equal(2, logs.Count());
    }

    [Fact]
    public async Task LogAsync_ShouldThrowArgumentNullException_WhenEntityIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.LogAsync<TestEntity>(null!, AuditActions.Create, "user123"));
    }

    [Fact]
    public async Task LogAsync_ShouldThrowArgumentException_WhenActionIsEmpty()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.LogAsync(entity, "", "user123"));
    }

    [Fact]
    public async Task LogAsync_ShouldThrowArgumentException_WhenUserIdIsEmpty()
    {
        // Arrange
        var entity = new TestEntity { Id = 1, Name = "Test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.LogAsync(entity, AuditActions.Create, ""));
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
