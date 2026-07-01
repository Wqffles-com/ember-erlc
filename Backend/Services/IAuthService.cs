namespace Backend.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string userName, string password);
    Task<AuthResult> LoginAsync(string userName, string password);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}

public record AuthResult(bool Success, string? AccessToken, string? RefreshToken, string? ErrorMessage);
