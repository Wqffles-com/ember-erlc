using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateServerRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    string? Description = null,
    string? IconUrl = null);

public record UpdateServerRequest(string? Name = null, string? Description = null, string? JoinCode = null, string? IconUrl = null);

public record AssignRoleRequest(Guid RoleId);
