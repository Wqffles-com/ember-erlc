using Backend.Data;
using Backend.Data.Models;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Backend.Tests;

public class ApplicationUserServiceTests
{
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock = new();

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        context.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), UserName = "user1", PasswordHash = "hash" });
        context.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), UserName = "user2", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var users = await service.GetAllAsync();

        Assert.Equal(2, users.Count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var id = Guid.NewGuid();
        context.Users.Add(new ApplicationUser { Id = id, UserName = "user1", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var user = await service.GetByIdAsync(id);

        Assert.NotNull(user);
        Assert.Equal("user1", user.UserName);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var user = await service.GetByIdAsync(Guid.NewGuid());

        Assert.Null(user);
    }

    [Fact]
    public async Task GetByUserNameAsync_ReturnsUser_WhenExists()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        context.Users.Add(new ApplicationUser { Id = Guid.NewGuid(), UserName = "testuser", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var user = await service.GetByUserNameAsync("testuser");

        Assert.NotNull(user);
        Assert.Equal("testuser", user.UserName);
    }

    [Fact]
    public async Task GetByUserNameAsync_ReturnsNull_WhenNotExists()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var user = await service.GetByUserNameAsync("nonexistent");

        Assert.Null(user);
    }

    [Fact]
    public async Task CreateAsync_AddsUser()
    {
        using var context = CreateContext();
        _passwordHasherMock.Setup(x => x.HashPassword(null!, "rawPassword")).Returns("hashedPassword");
        var service = CreateService(context);

        var user = await service.CreateAsync("newuser", "rawPassword");

        Assert.NotNull(await context.Users.FindAsync(user.Id));
        Assert.Equal("newuser", user.UserName);
        Assert.Equal("hashedPassword", user.PasswordHash);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUser()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var id = Guid.NewGuid();
        context.Users.Add(new ApplicationUser { Id = id, UserName = "oldname", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var user = await service.UpdateAsync(id, "newname");

        Assert.NotNull(user);
        Assert.Equal("newname", user.UserName);
        Assert.Equal("newname", (await context.Users.FindAsync(id))!.UserName);
    }

    [Fact]
    public async Task DeleteAsync_RemovesUser()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        var id = Guid.NewGuid();
        context.Users.Add(new ApplicationUser { Id = id, UserName = "user", PasswordHash = "hash" });
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(id);

        Assert.True(result);
        Assert.Null(await context.Users.FindAsync(id));
    }

    private ApplicationUserService CreateService(ApplicationDbContext context)
    {
        return new ApplicationUserService(context, _passwordHasherMock.Object);
    }
}
