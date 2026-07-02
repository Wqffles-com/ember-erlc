using Backend.Data;
using Backend.Data.Models;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ModerationLogService(ApplicationDbContext context) : IModerationLogService
{
    public async Task<PagedResponse<ModerationLog>> GetPagedAsync(
        Guid serverId, int page, int pageSize,
        ActionType? actionType = null,
        string? targetUserId = null,
        Guid? actorId = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var query = context.ModerationLogs
            .Include(m => m.Actor)
            .Where(m => m.ServerId == serverId && !m.IsDeleted);

        if (actionType.HasValue)
            query = query.Where(m => m.ActionType == actionType.Value);

        if (!string.IsNullOrWhiteSpace(targetUserId))
            query = query.Where(m => m.TargetUserId == targetUserId);

        if (actorId.HasValue)
            query = query.Where(m => m.ActorId == actorId.Value);

        if (from.HasValue)
            query = query.Where(m => m.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(m => m.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<ModerationLog>(items, page, pageSize, totalCount);
    }

    public async Task<ModerationLog?> GetByIdAsync(Guid id)
    {
        return await context.ModerationLogs
            .Include(m => m.Actor)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
    }

    public async Task<ModerationLog> CreateAsync(Guid serverId, Guid actorId, string targetUserId, string? targetUsername, ActionType actionType, string reason, string? evidence)
    {
        var log = new ModerationLog
        {
            Id = Guid.NewGuid(),
            ServerId = serverId,
            ActorId = actorId,
            TargetUserId = targetUserId,
            TargetUsername = targetUsername,
            ActionType = actionType,
            Reason = reason,
            Evidence = evidence,
            CreatedAt = DateTime.UtcNow
        };

        context.ModerationLogs.Add(log);
        await context.SaveChangesAsync();

        return log;
    }

    public async Task<ModerationLog?> UpdateAsync(Guid id, ActionType? actionType = null, string? reason = null, string? evidence = null)
    {
        var log = await context.ModerationLogs
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (log is null) return null;

        if (actionType.HasValue) log.ActionType = actionType.Value;
        if (reason is not null) log.Reason = reason.Length == 0 ? null : reason;
        if (evidence is not null) log.Evidence = evidence.Length == 0 ? null : evidence;

        await context.SaveChangesAsync();
        return log;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var log = await context.ModerationLogs
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        if (log is null) return false;

        log.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }
}
