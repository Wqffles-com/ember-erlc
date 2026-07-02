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
            .Include(s => s.Owner)
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == serverId);
    }

    public async Task<Server> CreateAsync(string name, Guid ownerId, string? description = null, string? iconUrl = null)
    {
        var joinCode = await GenerateUniqueJoinCodeAsync();

        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            JoinCode = joinCode,
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

    public async Task<Server?> UpdateAsync(Guid serverId, string? name = null, string? description = null, string? joinCode = null, string? iconUrl = null)
    {
        var server = await context.Servers.FindAsync(serverId);
        if (server is null) return null;

        if (name is not null) server.Name = name;
        if (description is not null) server.Description = description;
        if (joinCode is not null) server.JoinCode = joinCode;
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

    public async Task<JoinRequest> SubmitJoinRequestAsync(string joinCode, Guid userId)
    {
        var server = await context.Servers.FirstOrDefaultAsync(s => s.JoinCode == joinCode)
            ?? throw new Models.NotFoundException("Server not found.");

        var existingMember = await context.ServerMembers
            .FirstOrDefaultAsync(sm => sm.ServerId == server.Id && sm.UserId == userId);
        if (existingMember is not null)
            throw new Models.ConflictException("You are already a member of this server.");

        var existingRequest = await context.JoinRequests
            .FirstOrDefaultAsync(jr => jr.ServerId == server.Id && jr.UserId == userId && jr.Status == JoinRequestStatus.Pending);
        if (existingRequest is not null)
            throw new Models.ConflictException("You already have a pending join request.");

        var request = new JoinRequest
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            UserId = userId,
            Status = JoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        context.JoinRequests.Add(request);
        await context.SaveChangesAsync();

        await context.Entry(request).Reference(jr => jr.User).LoadAsync();
        return request;
    }

    public async Task<List<JoinRequest>> GetJoinRequestsAsync(Guid serverId)
    {
        return await context.JoinRequests
            .Include(jr => jr.User)
            .Where(jr => jr.ServerId == serverId && jr.Status == JoinRequestStatus.Pending)
            .ToListAsync();
    }

    public async Task<ServerMember?> AcceptJoinRequestAsync(Guid serverId, Guid requestId)
    {
        var request = await context.JoinRequests
            .FirstOrDefaultAsync(jr => jr.Id == requestId && jr.ServerId == serverId && jr.Status == JoinRequestStatus.Pending);

        if (request is null) return null;

        request.Status = JoinRequestStatus.Accepted;
        await context.SaveChangesAsync();

        var member = await AddMemberAsync(serverId, request.UserId);
        return member;
    }

    public async Task<bool> DenyJoinRequestAsync(Guid serverId, Guid requestId)
    {
        var request = await context.JoinRequests
            .FirstOrDefaultAsync(jr => jr.Id == requestId && jr.ServerId == serverId && jr.Status == JoinRequestStatus.Pending);

        if (request is null) return false;

        request.Status = JoinRequestStatus.Denied;
        await context.SaveChangesAsync();
        return true;
    }

    private async Task<ServerMember?> AddMemberAsync(Guid serverId, Guid userId)
    {
        var existing = await context.ServerMembers
            .Include(sm => sm.User)
            .FirstOrDefaultAsync(sm => sm.ServerId == serverId && sm.UserId == userId);
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

        await context.Entry(member).Reference(sm => sm.User).LoadAsync();
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

    private async Task<string> GenerateUniqueJoinCodeAsync()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        string code;
        do
        {
            code = new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        } while (await context.Servers.AnyAsync(s => s.JoinCode == code));
        return code;
    }
}
