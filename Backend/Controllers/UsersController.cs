using Backend.Extensions;
using Backend.Models;
using Backend.Models.Requests;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UsersController(IApplicationUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await this.GetCurrentUserAsync();
        return Ok(ApiResponse<UserDto>.Success(new UserDto(user.Id, user.UserName)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await userService.GetByIdAsync(id)
            ?? throw new NotFoundException("User not found.");

        return Ok(ApiResponse<UserDto>.Success(new UserDto(user.Id, user.UserName)));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var user = await userService.CreateAsync(request.UserName, request.Password);
        return CreatedAtAction(nameof(GetById), new { Id = user.Id },
            ApiResponse<UserDto>.Created(new UserDto(user.Id, user.UserName)));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
    {
        var userId = this.GetUserId();

        var user = await userService.UpdateAsync(userId, request.UserName)
            ?? throw new NotFoundException("User not found.");

        return Ok(ApiResponse<UserDto>.Success(new UserDto(user.Id, user.UserName)));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        var userId = this.GetUserId();

        var deleted = await userService.DeleteAsync(userId);
        if (!deleted)
            throw new NotFoundException("User not found.");

        return Ok(ApiResponse<object>.NoContent());
    }

}
