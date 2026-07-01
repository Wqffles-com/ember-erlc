using System.Security.Claims;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Backend.Authorization;

public class RequirePermissionAuthorizationHandler(
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<RequirePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RequirePermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null) return;

        if (!Guid.TryParse(userIdClaim, out var userId)) return;

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        var routeData = httpContext.GetRouteData();
        if (!routeData.Values.TryGetValue("serverId", out var serverIdObj)) return;

        if (serverIdObj is null) return;
        if (!Guid.TryParse(serverIdObj.ToString(), out var serverId)) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var member = await db.ServerMembers
            .Include(sm => sm.MemberRoles)
            .ThenInclude(mr => mr.Role)
            .FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);

        if (member is null) return;

        long effectivePermissions = 0;
        foreach (var mr in member.MemberRoles)
        {
            effectivePermissions |= mr.Role.Permissions;
        }

        if (effectivePermissions.HasAllPermissions(requirement.Permissions))
        {
            context.Succeed(requirement);
        }
    }
}
