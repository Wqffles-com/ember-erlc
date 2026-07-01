using System.ComponentModel.DataAnnotations;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] LoginRequest request)
    {
        var result = await authService.RegisterAsync(request.UserName, request.Password);
        if (!result.Success)
            return BadRequest(ApiResponseHelper.Failure(400, result.ErrorMessage));

        return Ok(ApiResponse<TokenResponse>.Success(
            new TokenResponse(result.AccessToken!, result.RefreshToken!)));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request.UserName, request.Password);
        if (!result.Success)
            return Unauthorized(ApiResponseHelper.Failure(401, result.ErrorMessage));

        return Ok(ApiResponse<TokenResponse>.Success(
            new TokenResponse(result.AccessToken!, result.RefreshToken!)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        if (!result.Success)
            return Unauthorized(ApiResponseHelper.Failure(401, result.ErrorMessage));

        return Ok(ApiResponse<TokenResponse>.Success(
            new TokenResponse(result.AccessToken!, result.RefreshToken!)));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest request)
    {
        await authService.LogoutAsync(request.RefreshToken);
        return Ok(ApiResponse<object>.NoContent());
    }
}

public record LoginRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(128, MinimumLength = 6)] string Password);

public record RefreshRequest(
    [Required] string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken);
