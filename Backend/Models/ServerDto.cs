namespace Backend.Models;

public record ServerDto(
    Guid Id,
    string Name,
    string? Description,
    string? JoinCode,
    Guid OwnerId,
    string? IconUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
