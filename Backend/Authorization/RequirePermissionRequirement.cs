using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

public class RequirePermissionRequirement(Permission[] permissions) : IAuthorizationRequirement
{
    public Permission[] Permissions { get; } = permissions;
}
