using Backend.Data;
using Backend.Data.Models;
using Backend.Options;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Backend.Tests;

public class AuthServiceTests
{
    private readonly Mock<IJwtService> _jwtServiceMock = new();
    private readonly Mock<IPasswordHasher<ApplicationUser>> _passwordHasherMock = new();
    private readonly Mock<IApplicationUserService> _userServiceMock = new();
    private readonly IOptions<JwtOptions> _jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions { 
        CertificatePath = "path", 
        Issuer = "issuer", 
        Audience = "audience" 
    });

    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ReturnsSuccess()
    {
        using var context = CreateContext();
        _userServiceMock.Setup(x => x.GetByUserNameAsync("user")).ReturnsAsync((ApplicationUser?)null);
        _userServiceMock.Setup(x => x.CreateAsync("user", "password")).ReturnsAsync(
            new ApplicationUser { Id = Guid.NewGuid(), UserName = "user", PasswordHash = "hashedpassword" });
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("access");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh");

        var service = new AuthService(context, _userServiceMock.Object, _jwtServiceMock.Object, _passwordHasherMock.Object, _jwtOptions);

        var result = await service.RegisterAsync("user", "password");

        Assert.True(result.Success);
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        _userServiceMock.Verify(x => x.CreateAsync("user", "password"), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        using var context = CreateContext();
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = "user", PasswordHash = "hashed" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        _userServiceMock.Setup(x => x.GetByUserNameAsync("user")).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(It.IsAny<ApplicationUser>(), "hashed", "password"))
            .Returns(PasswordVerificationResult.Success);
        _jwtServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<IEnumerable<System.Security.Claims.Claim>>())).Returns("access");
        _jwtServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh");

        var service = new AuthService(context, _userServiceMock.Object, _jwtServiceMock.Object, _passwordHasherMock.Object, _jwtOptions);

        var result = await service.LoginAsync("user", "password");

        Assert.True(result.Success);
        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
    }
}
