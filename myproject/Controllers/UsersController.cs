using System.Diagnostics;
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
  private readonly ILogger<UsersController> _logger;

  public UsersController(IUserService userService, ILogger<UsersController> logger)
  {
    this._userService = userService;
    this._logger = logger;
  }

  [HttpGet]
  [Authorize(Policy = "RequireOwnerRole")]
  public async Task<IActionResult> GetAll([FromQuery] QueryParameters parameters)
  {
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("GET /api/v1/users called with query: {@Parameters}", parameters);

    var result = await _userService.GetUsersAsync(parameters);

    stopwatch.Stop();
    _logger.LogInformation("GET /api/v1/users completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

    if (result.StatusCode != 200)
    {
      _logger.LogWarning("GET /api/v1/users returned {StatusCode}: {Message}", result.StatusCode, result.Message);
      return StatusCode(result.StatusCode, new { status = result.StatusCode, message = result.Message });
    }

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
    var stopwatch = Stopwatch.StartNew();
    _logger.LogInformation("GET /api/v1/user/@{Id} called with query: {@Id}", id);
    // Validate input
    if (id == Guid.Empty)
    {
      return BadRequest(new { status = 400, message = "Invalid user ID" });
    }

    var result = await _userService.GetUserAsync(id);

    stopwatch.Stop();
    _logger.LogInformation("GET /api/v1/user completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

    if (result.StatusCode != 200)
    {
      // This handles unexpected cases where status is 200 but data is null
      _logger.LogWarning("GET /api/v1/user returned {StatusCode}: {Message}", result.StatusCode, result.Message);
      return StatusCode(500, new { status = 500, message = result.Message });
    }

    return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message,
      data = result.Data ?? new object() // Ensure data is never null
    });
  }

  [HttpPost]
  [Authorize(Policy = "RequireOwnerAdminRole")]
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

  [HttpPost("bulk")]
  [Authorize(Policy = "RequireOwnerAdminRole")]
  public async Task<IActionResult> CreateUsers(IEnumerable<CreateUserDto> entities)
  {
    if (entities is null) return BadRequest(new { message = "Please enter your information." });
    var result = await _userService.AddUsersAsync(entities);

    return StatusCode(result.StatusCode, new
    {
      status = result.StatusCode,
      message = result.Message
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
