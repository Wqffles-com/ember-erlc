using System.Security.Claims;
using Backend.Data;
using Backend.Data.Models;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class RoleService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor) : IRoleService
{
    public async Task<List<Role>> GetAllForServerAsync(Guid serverId)
    {
        return await context.Roles
            .Where(r => r.ServerId == serverId)
            .OrderByDescending(r => r.Position)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdAsync(Guid roleId)
    {
        return await context.Roles.FindAsync(roleId);
    }

    public async Task<Role> CreateAsync(Guid serverId, string name, long permissions, int position = 0, string? color = null, bool isDefault = false)
    {
        await ThrowIfCantManagePositionAsync(serverId, position);

        if (isDefault)
        {
            var existingDefault = await context.Roles.FirstOrDefaultAsync(r => r.ServerId == serverId && r.IsDefault);
            if (existingDefault is not null)
                existingDefault.IsDefault = false;
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            Name = name,
            Color = color,
            Permissions = permissions,
            Position = position,
            IsDefault = isDefault,
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();

        return role;
    }

    public async Task<Role?> UpdateAsync(Guid roleId, string? name = null, long? permissions = null, int? position = null, string? color = null, bool? isDefault = null)
    {
        var role = await context.Roles.FindAsync(roleId);
        if (role is null) return null;

        var targetPosition = position ?? role.Position;
        await ThrowIfCantManagePositionAsync(role.ServerId, targetPosition);

        if (name is not null) role.Name = name;
        if (permissions.HasValue) role.Permissions = permissions.Value;
        if (position.HasValue) role.Position = position.Value;
        if (color is not null) role.Color = color;
        if (isDefault.HasValue && isDefault.Value)
        {
            var existingDefault = await context.Roles.FirstOrDefaultAsync(r => r.ServerId == role.ServerId && r.IsDefault && r.Id != roleId);
            if (existingDefault is not null)
                existingDefault.IsDefault = false;

            role.IsDefault = true;
        }

        await context.SaveChangesAsync();
        return role;
    }

    public async Task<bool> DeleteAsync(Guid roleId)
    {
        var role = await context.Roles.FindAsync(roleId);
        if (role is null) return false;

        await ThrowIfCantManagePositionAsync(role.ServerId, role.Position);

        context.Roles.Remove(role);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task AssignRoleToMemberAsync(Guid memberId, Guid roleId)
    {
        var role = await context.Roles.FindAsync(roleId);
        if (role is null) throw new NotFoundException("Role not found.");

        var member = await context.ServerMembers.FindAsync(memberId);
        if (member is null) throw new NotFoundException("Member not found.");

        await ThrowIfCantManagePositionAsync(member.ServerId, role.Position);

        var existing = await context.MemberRoles.FirstOrDefaultAsync(mr => mr.MemberId == memberId && mr.RoleId == roleId);
        if (existing is not null) return;

        context.MemberRoles.Add(new MemberRole
        {
            MemberId = memberId,
            RoleId = roleId
        });

        await context.SaveChangesAsync();
    }

    public async Task RemoveRoleFromMemberAsync(Guid memberId, Guid roleId)
    {
        var role = await context.Roles.FindAsync(roleId);
        if (role is null) throw new NotFoundException("Role not found.");

        var member = await context.ServerMembers.FindAsync(memberId);
        if (member is null) throw new NotFoundException("Member not found.");

        await ThrowIfCantManagePositionAsync(member.ServerId, role.Position);

        var memberRole = await context.MemberRoles.FirstOrDefaultAsync(mr => mr.MemberId == memberId && mr.RoleId == roleId);
        if (memberRole is not null)
        {
            context.MemberRoles.Remove(memberRole);
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<Role>> GetMemberRolesAsync(Guid memberId)
    {
        return await context.MemberRoles
            .Where(mr => mr.MemberId == memberId)
            .Select(mr => mr.Role)
            .ToListAsync();
    }

    public async Task<long> GetEffectivePermissionsAsync(Guid memberId)
    {
        var permissions = await context.MemberRoles
            .Where(mr => mr.MemberId == memberId)
            .Select(mr => mr.Role.Permissions)
            .ToListAsync();

        long effective = 0;
        foreach (var perm in permissions)
            effective |= perm;

        return effective;
    }

    private async Task ThrowIfCantManagePositionAsync(Guid serverId, int targetPosition)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return;

        var isOwner = await context.Servers.AnyAsync(s => s.Id == serverId && s.OwnerId == userId);
        if (isOwner) return;

        var effectivePerms = await GetCurrentUserPermissionsAsync(serverId);
        if ((effectivePerms & (long)Permission.Administrator) != 0) return;

        var maxPosition = await GetCurrentUserMaxPositionAsync(serverId);
        if (maxPosition <= targetPosition)
            throw new UnauthorizedException("You cannot manage roles at or above your highest role.");
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId)) return null;
        return userId;
    }

    private async Task<int> GetCurrentUserMaxPositionAsync(Guid serverId)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return -1;

        return await context.ServerMembers
            .Where(sm => sm.ServerId == serverId && sm.UserId == userId.Value)
            .SelectMany(sm => sm.MemberRoles)
            .Select(mr => mr.Role.Position)
            .DefaultIfEmpty(-1)
            .MaxAsync();
    }

    private async Task<long> GetCurrentUserPermissionsAsync(Guid serverId)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return 0;

        var permissions = await context.ServerMembers
            .Where(sm => sm.ServerId == serverId && sm.UserId == userId.Value)
            .SelectMany(sm => sm.MemberRoles)
            .Select(mr => mr.Role.Permissions)
            .ToListAsync();

        long effective = 0;
        foreach (var perm in permissions)
            effective |= perm;

        return effective;
    }
}
