using AuthService.DTOs.Users;
using AuthService.Common;
using AuthService.Infrastructure.Authorization;
using AuthService.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Pagination;

namespace AuthService.Controllers;

[ApiController]
[Authorize]
[ResourcePermissionAuthorize("users")]
[Route("api/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<UserResponse>>> GetAll([FromQuery] UserQueryParameters query, CancellationToken cancellationToken)
    {
        var response = await userService.GetAsync(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var response = await userService.GetByIdAsync(id, cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var response = await userService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        var response = await userService.UpdateAsync(actingUserId, id, request, cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var actingUserId = User.GetRequiredUserId();
        await userService.DeleteAsync(actingUserId, id, cancellationToken);
        return NoContent();
    }
}
