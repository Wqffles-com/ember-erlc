using System.Security.Claims;
using Backend.Data.Models;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Extensions;

public static class ControllerBaseExtensions
{
    public static async Task<ApplicationUser> GetCurrentUserAsync(this ControllerBase controller)
    {
        var userIdString = controller.User.FindFirstValue(JwtService.NameIdentifierClaimType)
            ?? throw new UnauthorizedException("User is not authorized.");

        var userId = Guid.Parse(userIdString);
        var userService = controller.HttpContext.RequestServices.GetRequiredService<IApplicationUserService>();

        return await userService.GetByIdAsync(userId)
            ?? throw new NotFoundException("User not found.");
    }

    public static Guid GetUserId(this ControllerBase controller)
    {
        return Guid.Parse(controller.User.FindFirstValue(JwtService.NameIdentifierClaimType)!);
    }
}
