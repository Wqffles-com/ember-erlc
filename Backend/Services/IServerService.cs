using Backend.Data.Models;

namespace Backend.Services;

public interface IServerService
{
    Task<List<Server>> GetAllForUserAsync(Guid userId);
    Task<Server?> GetByIdAsync(Guid serverId);
    Task<Server> CreateAsync(string name, Guid ownerId, string? description = null, string? iconUrl = null);
    Task<Server?> UpdateAsync(Guid serverId, string? name = null, string? description = null, string? joinCode = null, string? iconUrl = null);
    Task<bool> DeleteAsync(Guid serverId);

    Task<List<ServerMember>> GetMembersAsync(Guid serverId);
    Task<bool> RemoveMemberAsync(Guid serverId, Guid userId);
    Task<ServerMember?> GetMemberAsync(Guid serverId, Guid userId);

    Task<JoinRequest> SubmitJoinRequestAsync(string joinCode, Guid userId);
    Task<List<JoinRequest>> GetJoinRequestsAsync(Guid serverId);
    Task<ServerMember?> AcceptJoinRequestAsync(Guid serverId, Guid requestId);
    Task<bool> DenyJoinRequestAsync(Guid serverId, Guid requestId);
}
