using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateModLogRequest(
    [Required] string TargetUserId,
    string? TargetUsername,
    [Required] ActionType ActionType,
    [Required, StringLength(512, MinimumLength = 1)] string Reason,
    string? Evidence);

public record UpdateModLogRequest(
    ActionType? ActionType = null,
    string? Reason = null,
    string? Evidence = null);

public record ModLogFilterRequest(
    ActionType? ActionType = null,
    string? TargetUserId = null,
    Guid? ActorId = null,
    DateTime? From = null,
    DateTime? To = null);
