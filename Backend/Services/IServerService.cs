using Backend.Data.Models;

namespace Backend.Services;

public interface IServerService
{
    Task<List<Server>> GetAllForUserAsync(Guid userId);
    Task<Server?> GetByIdAsync(Guid serverId);
    Task<Server> CreateAsync(string name, Guid ownerId, string? description = null, string? robloxServerId = null, string? iconUrl = null);
    Task<Server?> UpdateAsync(Guid serverId, string? name = null, string? description = null, string? robloxServerId = null, string? iconUrl = null);
    Task<bool> DeleteAsync(Guid serverId);

    Task<List<ServerMember>> GetMembersAsync(Guid serverId);
    Task<ServerMember?> AddMemberAsync(Guid serverId, Guid userId);
    Task<bool> RemoveMemberAsync(Guid serverId, Guid userId);
    Task<ServerMember?> GetMemberAsync(Guid serverId, Guid userId);
}
