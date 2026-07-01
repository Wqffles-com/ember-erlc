using System.ComponentModel.DataAnnotations;
using Backend.Authorization;
using Backend.Data.Models;
using Backend.Models;
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
        return Ok(ApiResponse<List<Role>>.Success(roles));
    }

    [HttpGet("{roleId:guid}")]
    public async Task<IActionResult> GetById(Guid serverId, Guid roleId)
    {
        var role = await roleService.GetByIdAsync(roleId);
        if (role is null || role.ServerId != serverId)
            throw new NotFoundException("Role not found.");

        return Ok(ApiResponse<Role>.Success(role));
    }

    [HttpPost]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> Create(Guid serverId, [FromBody] CreateRoleRequest request)
    {
        var role = await roleService.CreateAsync(serverId, request.Name, request.Permissions, request.Position, request.Color, request.IsDefault);
        return CreatedAtAction(nameof(GetById), new { serverId, roleId = role.Id },
            ApiResponse<Role>.Created(role));
    }

    [HttpPut("{roleId:guid}")]
    [RequirePermission(Permission.ManageRoles)]
    public async Task<IActionResult> Update(Guid serverId, Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        var role = await roleService.UpdateAsync(roleId, request.Name, request.Permissions, request.Position, request.Color, request.IsDefault)
            ?? throw new NotFoundException("Role not found.");

        return Ok(ApiResponse<Role>.Success(role));
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

public record CreateRoleRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    long Permissions,
    int Position = 0,
    string? Color = null,
    bool IsDefault = false);
public record UpdateRoleRequest(string? Name = null, long? Permissions = null, int? Position = null, string? Color = null, bool? IsDefault = null);
