using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateRoleRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    long Permissions,
    int Position = 0,
    string? Color = null,
    bool IsDefault = false);

public record UpdateRoleRequest(string? Name = null, long? Permissions = null, int? Position = null, string? Color = null, bool? IsDefault = null);
