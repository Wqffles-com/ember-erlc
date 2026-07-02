using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record LoginRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(128, MinimumLength = 6)] string Password);

public record RefreshRequest(
    [Required] string RefreshToken);

public record TokenResponse(string AccessToken, string RefreshToken);
