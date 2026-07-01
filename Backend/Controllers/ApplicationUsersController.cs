using System.ComponentModel.DataAnnotations;
using Backend.Data.Models;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class ApplicationUsersController(IApplicationUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await userService.GetAllAsync();
        return Ok(ApiResponse<List<ApplicationUser>>.Success(users));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id)
            ?? throw new NotFoundException("User not found.");

        return Ok(ApiResponse<ApplicationUser>.Success(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request.UserName, request.Password);
        return CreatedAtAction(nameof(GetById), new { Id = user.Id },
            ApiResponse<ApplicationUser>.Created(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var user = await userService.UpdateAsync(id, request.UserName)
            ?? throw new NotFoundException("User not found.");

        return Ok(ApiResponse<ApplicationUser>.Success(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await userService.DeleteAsync(id);
        if (!deleted)
            throw new NotFoundException("User not found.");

        return Ok(ApiResponse<object>.NoContent());
    }
}

public record CreateUserRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(128, MinimumLength = 6)] string Password);

public record UpdateUserRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName);
