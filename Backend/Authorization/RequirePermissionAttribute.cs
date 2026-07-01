using Backend.Models;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermissionAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    public Permission[] Permissions { get; }

    public RequirePermissionAttribute(params Permission[] permissions)
    {
        Permissions = permissions;
    }

    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new RequirePermissionRequirement(Permissions);
    }
}
