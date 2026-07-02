namespace Backend.Models;

public record ServerDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? JoinCode,
    Guid OwnerId,
    string OwnerName,
    string? IconUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt);
