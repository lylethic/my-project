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
  public async Task<IActionResult> GetAll([FromQuery] QueryParameters parameters)
  {
    var result = await _userService.GetUsersAsync(parameters);

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
      data = new
      {
        users = result.Data?.Items,
        pagination = new
        {
          totalItems = result.Data?.TotalItems,
          pageNumber = result.Data?.PageNumber,
          pageSize = result.Data?.PageSize,
          totalPages = result.Data?.TotalPages
        }
      }
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

  [Authorize(Policy = "RequireOwnerAdminRole")]
  [HttpPost("import")]
  public async Task<IActionResult> ImportUsers(IFormFile file)
  {

    if (file == null || file.Length == 0)
      return BadRequest("No file uploaded.");

    using var stream = file.OpenReadStream();
    var result = await _userService.AddUsersFromExcelAsync(stream);

    if (result.StatusCode == 200)
      return Ok(new
      {
        status = result.StatusCode,
        message = result.Message
      });

    return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message
    });
  }

  [Authorize(Policy = "RequireOwnerAdminRole")]
  [HttpGet("export")]
  public async Task<IActionResult> ExportUsersToExcel([FromQuery] int take = 20, [FromQuery] Guid? roleId = null)
  {
    try
    {
      var fileBytes = await _userService.ExportUsersToExcelAsync(take, roleId);

      var fileName = $"users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
      return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { message = "Internal server error while exporting users." });
      throw new Exception(ex.Message);
    }
  }
}
