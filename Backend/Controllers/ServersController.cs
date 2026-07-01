using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Backend.Authorization;
using Backend.Data.Models;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ServersController(IServerService serverService, IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetUserId();
        var servers = await serverService.GetAllForUserAsync(userId);
        return Ok(ApiResponse<List<Server>>.Success(servers));
    }

    [HttpGet("{serverId:guid}")]
    public async Task<IActionResult> GetById(Guid serverId)
    {
        var server = await serverService.GetByIdAsync(serverId)
            ?? throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<Server>.Success(server));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServerRequest request)
    {
        var userId = GetUserId();
        var server = await serverService.CreateAsync(request.Name, userId, request.Description, request.RobloxServerId, request.IconUrl);
        return CreatedAtAction(nameof(GetById), new { serverId = server.Id },
            ApiResponse<Server>.Created(server));
    }

    [HttpPut("{serverId:guid}")]
    [RequirePermission(Permission.ManageServer)]
    public async Task<IActionResult> Update(Guid serverId, [FromBody] UpdateServerRequest request)
    {
        var server = await serverService.UpdateAsync(serverId, request.Name, request.Description, request.RobloxServerId, request.IconUrl)
            ?? throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<Server>.Success(server));
    }

    [HttpDelete("{serverId:guid}")]
    [RequirePermission(Permission.ManageServer)]
    public async Task<IActionResult> Delete(Guid serverId)
    {
        var deleted = await serverService.DeleteAsync(serverId);
        if (!deleted)
            throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<object>.NoContent());
    }

    [HttpGet("{serverId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid serverId)
    {
        var server = await serverService.GetByIdAsync(serverId)
            ?? throw new NotFoundException("Server not found.");

        var members = await serverService.GetMembersAsync(serverId);
        return Ok(ApiResponse<List<ServerMember>>.Success(members));
    }

    [HttpPost("{serverId:guid}/members")]
    [RequirePermission(Permission.ManageMembers)]
    public async Task<IActionResult> AddMember(Guid serverId, [FromBody] AddMemberRequest request)
    {
        var member = await serverService.AddMemberAsync(serverId, request.UserId)
            ?? throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<ServerMember>.Success(member));
    }

    [HttpDelete("{serverId:guid}/members/{userId:guid}")]
    [RequirePermission(Permission.ManageMembers)]
    public async Task<IActionResult> RemoveMember(Guid serverId, Guid userId)
    {
        var removed = await serverService.RemoveMemberAsync(serverId, userId);
        if (!removed)
            throw new NotFoundException("Member not found or cannot remove the owner.");

        return Ok(ApiResponse<object>.NoContent());
    }

    [HttpGet("{serverId:guid}/members/{memberId:guid}/roles")]
    public async Task<IActionResult> GetMemberRoles(Guid serverId, Guid memberId)
    {
        var roles = await roleService.GetMemberRolesAsync(memberId);
        return Ok(ApiResponse<List<Role>>.Success(roles));
    }

    [HttpPost("{serverId:guid}/members/{memberId:guid}/roles")]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> AssignRole(Guid serverId, Guid memberId, [FromBody] AssignRoleRequest request)
    {
        await roleService.AssignRoleToMemberAsync(memberId, request.RoleId);
        return Ok(ApiResponse<object>.NoContent());
    }

    [HttpDelete("{serverId:guid}/members/{memberId:guid}/roles/{roleId:guid}")]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> RemoveRole(Guid serverId, Guid memberId, Guid roleId)
    {
        await roleService.RemoveRoleFromMemberAsync(memberId, roleId);
        return Ok(ApiResponse<object>.NoContent());
    }

    [HttpGet("{serverId:guid}/members/{memberId:guid}/permissions")]
    public async Task<IActionResult> GetEffectivePermissions(Guid serverId, Guid memberId)
    {
        var permissions = await roleService.GetEffectivePermissionsAsync(memberId);
        var list = permissions.GetAllPermissions();
        return Ok(ApiResponse<List<Permission>>.Success(list));
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(JwtService.NameIdentifierClaimType)!);
    }
}

public record CreateServerRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    string? Description = null,
    string? RobloxServerId = null,
    string? IconUrl = null);
public record UpdateServerRequest(string? Name = null, string? Description = null, string? RobloxServerId = null, string? IconUrl = null);
public record AddMemberRequest(Guid UserId);
public record AssignRoleRequest(Guid RoleId);
