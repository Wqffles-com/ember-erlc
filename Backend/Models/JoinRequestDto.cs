using Backend.Data.Models;

namespace Backend.Models;

public record JoinRequestDto(
    Guid Id,
    Guid ServerId,
    Guid UserId,
    string UserName,
    JoinRequestStatus Status,
    DateTime CreatedAt);
