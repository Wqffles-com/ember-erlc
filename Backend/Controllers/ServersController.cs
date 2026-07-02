using Backend.Authorization;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Requests;
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
        var userId = this.GetUserId();
        var servers = await serverService.GetAllForUserAsync(userId);
        return Ok(ApiResponse<List<ServerDto>>.Success(servers.Select(s => new ServerDto(s.Id, s.Name, s.Description, s.JoinCode, s.OwnerId, s.IconUrl, s.CreatedAt, s.UpdatedAt)).ToList()));
    }

    [HttpGet("{serverId:guid}")]
    public async Task<IActionResult> GetById(Guid serverId)
    {
        var server = await serverService.GetByIdAsync(serverId)
            ?? throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<ServerDetailDto>.Success(new ServerDetailDto(
            server.Id, server.Name, server.Description,
            server.JoinCode, server.OwnerId, server.Owner?.UserName ?? "Unknown",
            server.IconUrl, server.CreatedAt, server.UpdatedAt)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServerRequest request)
    {
        var userId = this.GetUserId();
        var server = await serverService.CreateAsync(request.Name, userId, request.Description, request.IconUrl);
        return CreatedAtAction(nameof(GetById), new { serverId = server.Id },
            ApiResponse<ServerDto>.Created(new ServerDto(
                server.Id, server.Name, server.Description,
                server.JoinCode, server.OwnerId, server.IconUrl,
                server.CreatedAt, server.UpdatedAt)));
    }

    [HttpPut("{serverId:guid}")]
    [RequirePermission(Permission.ManageServer)]
    public async Task<IActionResult> Update(Guid serverId, [FromBody] UpdateServerRequest request)
    {
        var server = await serverService.UpdateAsync(serverId, request.Name, request.Description, request.JoinCode, request.IconUrl)
            ?? throw new NotFoundException("Server not found.");

        return Ok(ApiResponse<ServerDto>.Success(new ServerDto(
            server.Id, server.Name, server.Description,
            server.JoinCode, server.OwnerId, server.IconUrl,
            server.CreatedAt, server.UpdatedAt)));
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
        return Ok(ApiResponse<List<ServerMemberDto>>.Success(members.Select(m => new ServerMemberDto(
            m.Id, m.ServerId, m.UserId, m.User?.UserName ?? "Unknown", m.JoinedAt)).ToList()));
    }

    [HttpPost("join/{joinCode}")]
    public async Task<IActionResult> Join(string joinCode)
    {
        var userId = this.GetUserId();
        var joinRequest = await serverService.SubmitJoinRequestAsync(joinCode, userId);
        return Ok(ApiResponse<JoinRequestDto>.Success(new JoinRequestDto(
            joinRequest.Id, joinRequest.ServerId, joinRequest.UserId,
            joinRequest.User?.UserName ?? "Unknown",
            joinRequest.Status, joinRequest.CreatedAt)));
    }

    [HttpGet("{serverId:guid}/join-requests")]
    [RequirePermission(Permission.ManageMembers)]
    public async Task<IActionResult> GetJoinRequests(Guid serverId)
    {
        var requests = await serverService.GetJoinRequestsAsync(serverId);
        return Ok(ApiResponse<List<JoinRequestDto>>.Success(requests.Select(r => new JoinRequestDto(
            r.Id, r.ServerId, r.UserId, r.User?.UserName ?? "Unknown",
            r.Status, r.CreatedAt)).ToList()));
    }

    [HttpPost("{serverId:guid}/join-requests/{requestId:guid}/accept")]
    [RequirePermission(Permission.ManageMembers)]
    public async Task<IActionResult> AcceptJoinRequest(Guid serverId, Guid requestId)
    {
        var member = await serverService.AcceptJoinRequestAsync(serverId, requestId)
            ?? throw new NotFoundException("Join request not found.");

        return Ok(ApiResponse<ServerMemberDto>.Success(new ServerMemberDto(
            member.Id, member.ServerId, member.UserId,
            member.User?.UserName ?? "Unknown", member.JoinedAt)));
    }

    [HttpPost("{serverId:guid}/join-requests/{requestId:guid}/deny")]
    [RequirePermission(Permission.ManageMembers)]
    public async Task<IActionResult> DenyJoinRequest(Guid serverId, Guid requestId)
    {
        var denied = await serverService.DenyJoinRequestAsync(serverId, requestId);
        if (!denied)
            throw new NotFoundException("Join request not found.");

        return Ok(ApiResponse<object>.NoContent());
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
        return Ok(ApiResponse<List<RoleDto>>.Success(roles.Select(r => new RoleDto(
            r.Id, r.ServerId, r.Name, r.Color, r.Permissions,
            r.Position, r.IsDefault, r.CreatedAt)).ToList()));
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

}
