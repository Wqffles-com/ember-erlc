namespace Backend.Models;

public record RoleDto(
    Guid Id,
    Guid ServerId,
    string Name,
    string? Color,
    long Permissions,
    int Position,
    bool IsDefault,
    DateTime CreatedAt);
