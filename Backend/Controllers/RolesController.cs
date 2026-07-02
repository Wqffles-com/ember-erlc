using Backend.Authorization;
using Backend.Models;
using Backend.Models.Requests;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("servers/{serverId:guid}/[controller]")]
[Authorize]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid serverId)
    {
        var roles = await roleService.GetAllForServerAsync(serverId);
        return Ok(ApiResponse<List<RoleDto>>.Success(roles.Select(r => new RoleDto(
            r.Id, r.ServerId, r.Name, r.Color, r.Permissions,
            r.Position, r.IsDefault, r.CreatedAt)).ToList()));
    }

    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetById(Guid serverId, Guid roleId)
    {
        var role = await roleService.GetByIdAsync(roleId);
        if (role is null || role.ServerId != serverId)
            throw new NotFoundException("Role not found.");

        return Ok(ApiResponse<RoleDto>.Success(new RoleDto(
            role.Id, role.ServerId, role.Name, role.Color, role.Permissions,
            role.Position, role.IsDefault, role.CreatedAt)));
    }

    [HttpPost]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> Create(Guid serverId, [FromBody] CreateRoleRequest request)
    {
        var role = await roleService.CreateAsync(serverId, request.Name, request.Permissions, request.Position, request.Color, request.IsDefault);
        return CreatedAtAction(nameof(GetById), new { serverId, roleId = role.Id },
            ApiResponse<RoleDto>.Created(new RoleDto(
                role.Id, role.ServerId, role.Name, role.Color, role.Permissions,
                role.Position, role.IsDefault, role.CreatedAt)));
    }

    [HttpPut("{roleId:guid}")]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> Update(Guid serverId, Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        var role = await roleService.UpdateAsync(roleId, request.Name, request.Permissions, request.Position, request.Color, request.IsDefault)
            ?? throw new NotFoundException("Role not found.");

        return Ok(ApiResponse<RoleDto>.Success(new RoleDto(
            role.Id, role.ServerId, role.Name, role.Color, role.Permissions,
            role.Position, role.IsDefault, role.CreatedAt)));
    }

    [HttpDelete("{roleId:guid}")]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> Delete(Guid serverId, Guid roleId)
    {
        var deleted = await roleService.DeleteAsync(roleId);
        if (!deleted)
            throw new NotFoundException("Role not found.");

        return Ok(ApiResponse<object>.NoContent());
    }
}
