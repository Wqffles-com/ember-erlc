using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Requests;

public record CreateUserRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(128, MinimumLength = 6)] string Password);

public record UpdateUserRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName);
