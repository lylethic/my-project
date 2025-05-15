using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers;

[Route("api/v1/users")]
[ApiController]
[Authorize] // Requires authentication for all endpoints
public class UsersController : ControllerBase
{
  private readonly IUserService _userService;

  public UsersController(IUserService userService)
  {
    this._userService = userService;
  }

  [HttpGet]
  [Authorize(Policy = "RequireOwnerRole")]
  public async Task<IActionResult> GetAll([FromQuery] bool? isActive = true)
  {
    var result = await _userService.GetUsersAsync(isActive);

    if (result.StatusCode != 200)
      return StatusCode(result.StatusCode, new
      {
        status = result.StatusCode,
        message = result.Message
      });

    return Ok(new
    {
      status = result.StatusCode,
      message = result.Message,
      data = result.ListData
    });
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetUser(Guid id)
  {
    // Validate input
    if (id == Guid.Empty)
    {
      return BadRequest(new { status = 400, message = "Invalid user ID" });
    }

    var result = await _userService.GetUserAsync(id);

    if (result.Data == null && result.StatusCode == 200)
    {
      // This handles unexpected cases where status is 200 but data is null
      return StatusCode(500, new { status = 500, message = "Unexpected error" });
    }

    return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message,
      data = result.Data ?? new object() // Ensure data is never null
    });
  }

  [HttpPost]
  public async Task<IActionResult> CreateUser(CreateUserDto entity)
  {
    if (entity is null) return BadRequest(new { message = "Please enter your information." });
    var result = await _userService.AddUserAsync(entity);

    if (result.StatusCode != 200)
    {
      return StatusCode(result.StatusCode, new
      {
        status = result.StatusCode,
        message = result.Message
      });
    }

    return Ok(new
    {
      status = result.StatusCode,
      message = result.Message,
      data = result.Data
    });
  }

  [HttpDelete("{id}")]
  [Authorize(Policy = "RequireOwnerAdminRole")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var result = await _userService.DeleteUserAsync(id);
    if (result.StatusCode != 200) return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message
    });

    return NoContent();
  }

  [HttpPatch("{id}")]
  public async Task<IActionResult> Update(Guid id, UpdateUserDto entity)
  {
    var result = await _userService.UpdateUserAsync(id, entity);
    if (result.StatusCode == 404) return NotFound(result);

    return NoContent();
  }
}
