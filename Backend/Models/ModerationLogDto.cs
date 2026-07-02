namespace Backend.Models;

public record ModerationLogDto(
    Guid Id,
    Guid ServerId,
    Guid ActorId,
    string ActorName,
    string TargetUserId,
    string? TargetUsername,
    ActionType ActionType,
    string Reason,
    string? Evidence,
    DateTime CreatedAt);
