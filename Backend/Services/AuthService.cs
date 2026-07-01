using System.Security.Claims;
using Backend.Data;
using Backend.Data.Models;
using Backend.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

public class AuthService(
    ApplicationDbContext context,
    IApplicationUserService userService,
    IJwtService jwtService,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private const int MaxActiveRefreshTokens = 5;

    public async Task<AuthResult> RegisterAsync(string userName, string password)
    {
        var existingUser = await userService.GetByUserNameAsync(userName);
        if (existingUser is not null)
            return new AuthResult(false, null, null, "Username already taken.");

        var user = await userService.CreateAsync(userName, password);

        return await GenerateAuthResultAsync(user);
    }

    public async Task<AuthResult> LoginAsync(string userName, string password)
    {
        var user = await userService.GetByUserNameAsync(userName);
        if (user is null)
            return new AuthResult(false, null, null, "Invalid credentials.");

        var result = passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, password);
        if (result != PasswordVerificationResult.Success)
            return new AuthResult(false, null, null, "Invalid credentials.");

        return await GenerateAuthResultAsync(user);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var storedToken = await context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (storedToken is null || !storedToken.IsActive)
            return new AuthResult(false, null, null, "Invalid or expired refresh token.");

        storedToken.RevokedAt = DateTime.UtcNow;

        return await GenerateAuthResultAsync(storedToken.User);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
        if (storedToken is not null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }

    private async Task<AuthResult> GenerateAuthResultAsync(ApplicationUser user)
    {
        var now = DateTime.UtcNow;

        var staleTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && (rt.RevokedAt != null || rt.ExpiresAt <= now))
            .ToListAsync();
        if (staleTokens.Count > 0)
            context.RefreshTokens.RemoveRange(staleTokens);

        var activeTokens = await context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null && rt.ExpiresAt > now)
            .OrderBy(rt => rt.CreatedAt)
            .ToListAsync();

        var excess = activeTokens.Count - MaxActiveRefreshTokens + 1;
        for (int i = 0; i < excess; i++)
            activeTokens[i].RevokedAt = now;

        var claims = new List<Claim>
        {
            new(JwtService.NameIdentifierClaimType, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName)
        };

        var accessToken = jwtService.GenerateAccessToken(claims);
        var refreshTokenValue = jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshTokenValue,
            UserId = user.Id,
            CreatedAt = now,
            ExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenExpirationDays)
        };

        context.RefreshTokens.Add(refreshToken);
        await context.SaveChangesAsync();

        return new AuthResult(true, accessToken, refreshTokenValue, null);
    }
}
