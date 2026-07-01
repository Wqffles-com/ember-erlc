using Backend.Data;
using Backend.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ServerService(ApplicationDbContext context) : IServerService
{
    public async Task<List<Server>> GetAllForUserAsync(Guid userId)
    {
        return await context.Servers
            .Where(s => s.OwnerId == userId || s.Members.Any(m => m.UserId == userId))
            .ToListAsync();
    }

    public async Task<Server?> GetByIdAsync(Guid serverId)
    {
        return await context.Servers
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == serverId);
    }

    public async Task<Server> CreateAsync(string name, Guid ownerId, string? description = null, string? robloxServerId = null, string? iconUrl = null)
    {
        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            RobloxServerId = robloxServerId,
            OwnerId = ownerId,
            IconUrl = iconUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Servers.Add(server);

        var ownerRole = new Role
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            Name = "Owner",
            Color = "#FF0000",
            Permissions = (long)Models.Permission.Administrator,
            Position = int.MaxValue,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.Add(ownerRole);

        var defaultRole = new Role
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            Name = "Member",
            Color = null,
            Permissions = (long)(Models.Permission.ViewServer | Models.Permission.ViewMembers),
            Position = 0,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Roles.Add(defaultRole);

        var ownerMember = new ServerMember
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            UserId = ownerId,
            JoinedAt = DateTime.UtcNow
        };

        context.ServerMembers.Add(ownerMember);

        context.MemberRoles.Add(new MemberRole
        {
            MemberId = ownerMember.Id,
            RoleId = ownerRole.Id
        });

        context.MemberRoles.Add(new MemberRole
        {
            MemberId = ownerMember.Id,
            RoleId = defaultRole.Id
        });

        await context.SaveChangesAsync();

        return server;
    }

    public async Task<Server?> UpdateAsync(Guid serverId, string? name = null, string? description = null, string? robloxServerId = null, string? iconUrl = null)
    {
        var server = await context.Servers.FindAsync(serverId);
        if (server is null) return null;

        if (name is not null) server.Name = name;
        if (description is not null) server.Description = description;
        if (robloxServerId is not null) server.RobloxServerId = robloxServerId;
        if (iconUrl is not null) server.IconUrl = iconUrl;
        server.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return server;
    }

    public async Task<bool> DeleteAsync(Guid serverId)
    {
        var server = await context.Servers.FindAsync(serverId);
        if (server is null) return false;

        context.Servers.Remove(server);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ServerMember>> GetMembersAsync(Guid serverId)
    {
        return await context.ServerMembers
            .Include(sm => sm.User)
            .Where(sm => sm.ServerId == serverId)
            .ToListAsync();
    }

    public async Task<ServerMember?> AddMemberAsync(Guid serverId, Guid userId)
    {
        var server = await context.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (server is null) return null;

        var existing = await context.ServerMembers.FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);
        if (existing is not null) return existing;

        var member = new ServerMember
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        context.ServerMembers.Add(member);

        var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.ServerId == serverId && r.IsDefault);
        if (defaultRole is not null)
        {
            context.MemberRoles.Add(new MemberRole
            {
                MemberId = member.Id,
                RoleId = defaultRole.Id
            });
        }

        await context.SaveChangesAsync();
        return member;
    }

    public async Task<bool> RemoveMemberAsync(Guid serverId, Guid userId)
    {
        var server = await context.Servers.FirstOrDefaultAsync(s => s.Id == serverId);
        if (server is null) return false;

        if (server.OwnerId == userId) return false;

        var member = await context.ServerMembers.FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);
        if (member is null) return false;

        context.ServerMembers.Remove(member);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ServerMember?> GetMemberAsync(Guid serverId, Guid userId)
    {
        return await context.ServerMembers
            .Include(sm => sm.User)
            .Include(sm => sm.MemberRoles)
            .ThenInclude(mr => mr.Role)
            .FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);
    }
}
