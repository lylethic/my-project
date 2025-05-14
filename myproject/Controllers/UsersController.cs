using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers;

[Route("api/v1/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
  private readonly IUserService _userService;

  public UsersController(IUserService userService)
  {
    this._userService = userService;
  }

  [HttpGet]
  public async Task<IActionResult> GetAll([FromQuery] bool? isActive = true)
  {
    var users = await _userService.GetUsersAsync(isActive);

    if (users.StatusCode != 200)
      return NotFound(new
      {
        status = users.StatusCode,
        message = users.Message
      });

    return Ok(new
    {
      message = users.Message,
      data = users.ListData
    });
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> GetUser(Guid id)
  {
    var user = await _userService.GetUserAsync(id);

    if (user is not null)
      return Ok(new
      {
        message = user.Message,
        data = user.Data
      });

    return NotFound(user);
  }

  [HttpPost]
  public async Task<IActionResult> CreateUser(CreateUserDto entity)
  {
    if (entity is null) return BadRequest(new { message = "Please enter your information." });
    var user = await _userService.AddUserAsync(entity);

    if (user.StatusCode != 200) { return BadRequest(user); }

    return Ok(new
    {
      message = user.Message,
      data = user.Data
    });
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(Guid id)
  {
    var user = await _userService.DeleteUserAsync(id);
    if (user is null) return NotFound(user);

    return NoContent();
  }

  [HttpPatch("{id}")]
  public async Task<IActionResult> Update(Guid id, UpdateUserDto entity)
  {
    var user = await _userService.UpdateUserAsync(id, entity);
    if (user.StatusCode == 404) return NotFound(user);

    return NoContent();
  }
}
