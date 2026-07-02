using Backend.Data.Models;
using Backend.Models;

namespace Backend.Services;

public interface IModerationLogService
{
    Task<PagedResponse<ModerationLog>> GetPagedAsync(
        Guid serverId, int page, int pageSize,
        ActionType? actionType = null,
        string? targetUserId = null,
        Guid? actorId = null,
        DateTime? from = null,
        DateTime? to = null);

    Task<ModerationLog?> GetByIdAsync(Guid id);
    Task<ModerationLog> CreateAsync(Guid serverId, Guid actorId, string targetUserId, string? targetUsername, ActionType actionType, string reason, string? evidence);
    Task<ModerationLog?> UpdateAsync(Guid id, ActionType? actionType = null, string? reason = null, string? evidence = null);
    Task<bool> DeleteAsync(Guid id);
}
