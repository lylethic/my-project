using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Infrastructure.Services;

namespace Play.APIs.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
    {
        try
        {
            var result = await _userService.GetUsersAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _userService.GetById(id);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _userService.CreateUserAsync(request);
            return Created();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        try
        {
            await _userService.ImportUsersAsync(file);
            return Ok("Users imported successfully");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportUsers(bool? isActive = null, int? maxRows = null)
    {
        try
        {
            var filePath = await _userService.ExportUsersAsync(isActive, maxRows);
            return PhysicalFile(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Users.xlsx", true);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            // Log the exception
            return StatusCode(500, "An error occurred while exporting users.");
        }

    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateUserRequest request)
    {
        try
        {
            await _userService.UpdateUserAsync(id, request);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user." });
        }
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            await _userService.DeleteUserAsync(id);
            return Ok(new { message = "User deleted successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return BadRequest(new { message = "An error occurred while deleting the user." });
        }
    }
}
