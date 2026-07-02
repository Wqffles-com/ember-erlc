using Backend.Authorization;
using Backend.Data.Models;
using Backend.Extensions;
using Backend.Models;
using Backend.Models.Requests;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("servers/{serverId:guid}/mod-logs")]
[Authorize]
public class ModLogsController(IModerationLogService modLogService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(Permission.ViewModLogs)]
    public async Task<IActionResult> GetAll(
        Guid serverId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ActionType? actionType = null,
        [FromQuery] string? targetUserId = null,
        [FromQuery] Guid? actorId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await modLogService.GetPagedAsync(serverId, page, pageSize, actionType, targetUserId, actorId, from, to);
        var dto = new PagedResponse<ModerationLogDto>(
            result.Items.Select(MapToDto).ToList(),
            result.Page, result.PageSize, result.TotalCount);

        return Ok(ApiResponse<PagedResponse<ModerationLogDto>>.Success(dto));
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(Permission.ViewModLogs)]
    public async Task<IActionResult> GetById(Guid serverId, Guid id)
    {
        var log = await modLogService.GetByIdAsync(id);
        if (log is null || log.ServerId != serverId)
            throw new NotFoundException("Moderation log not found.");

        return Ok(ApiResponse<ModerationLogDto>.Success(MapToDto(log)));
    }

    [HttpPost]
    [RequirePermission(Permission.ManageModLogs)]
    public async Task<IActionResult> Create(Guid serverId, [FromBody] CreateModLogRequest request)
    {
        var actorId = this.GetUserId();
        var log = await modLogService.CreateAsync(serverId, actorId, request.TargetUserId, request.TargetUsername, request.ActionType, request.Reason, request.Evidence);
        return CreatedAtAction(nameof(GetById), new { serverId, id = log.Id },
            ApiResponse<ModerationLogDto>.Created(MapToDto(log)));
    }

    [HttpPatch("{id:guid}")]
    [RequirePermission(Permission.ManageModLogs)]
    public async Task<IActionResult> Update(Guid serverId, Guid id, [FromBody] UpdateModLogRequest request)
    {
        var log = await modLogService.GetByIdAsync(id);
        if (log is null || log.ServerId != serverId)
            throw new NotFoundException("Moderation log not found.");

        log = await modLogService.UpdateAsync(id, request.ActionType, request.Reason, request.Evidence);

        return Ok(ApiResponse<ModerationLogDto>.Success(MapToDto(log!)));
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(Permission.ManageModLogs)]
    public async Task<IActionResult> Delete(Guid serverId, Guid id)
    {
        var log = await modLogService.GetByIdAsync(id);
        if (log is null || log.ServerId != serverId)
            throw new NotFoundException("Moderation log not found.");

        await modLogService.DeleteAsync(id);
        return Ok(ApiResponse<object>.NoContent());
    }

    private static ModerationLogDto MapToDto(ModerationLog log)
    {
        return new ModerationLogDto(
            log.Id, log.ServerId, log.ActorId,
            log.Actor.UserName,
            log.TargetUserId, log.TargetUsername,
            log.ActionType, log.Reason, log.Evidence,
            log.CreatedAt);
    }
}
