namespace Backend.Models;

public record ServerMemberDto(
    Guid Id,
    Guid ServerId,
    Guid UserId,
    string UserName,
    DateTime JoinedAt);
