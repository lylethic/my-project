using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.API.IRepository;
using Play.Application.DTOs;
using Play.Infrastructure.Services;
using System.Diagnostics;

namespace Play.APIs.Controllers;

[Route("api/users")]
[ApiController]
//[Authorize] // Requires authentication for all endpoints
public class UsersController : ControllerBase
{
  private readonly UserService _userService;
  private readonly ILogger<UsersController> _logger;

  public UsersController(UserService userService, ILogger<UsersController> logger)
  {
    _userService = userService;
    _logger = logger;
  }

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


  // [HttpGet]
  // [Authorize(Policy = "RequireOwnerRole")]
  // public async Task<IActionResult> GetAll([FromQuery] QueryParameters parameters)
  // {
  //   var stopwatch = Stopwatch.StartNew();
  //   _logger.LogInformation("GET /api/v1/users called with query: {@Parameters}", parameters);

  //   var result = await _userService.GetUsersAsync(parameters);

  //   stopwatch.Stop();
  //   _logger.LogInformation("GET /api/v1/users completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

  //   if (result.StatusCode != 200)
  //   {
  //     _logger.LogWarning("GET /api/v1/users returned {StatusCode}: {Message}", result.StatusCode, result.Message);
  //     return StatusCode(result.StatusCode, new { status = result.StatusCode, message = result.Message });
  //   }

  //   return Ok(new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message,
  //     data = new
  //     {
  //       users = result.Data?.Data,
  //       pagination = new
  //       {
  //         pageSize = result.Data?.PageSize,
  //       }
  //     }
  //   });
  // }

  // [HttpGet("{id}")]
  // public async Task<IActionResult> GetUser(string id)
  // {
  //   var stopwatch = Stopwatch.StartNew();
  //   _logger.LogInformation("GET /api/v1/user/{Id} called with query: {QueryId}", id, id);
  //   // Validate input
  //   if (string.IsNullOrWhiteSpace(id))
  //   {
  //     return BadRequest(new { status = 400, message = "Invalid user ID" });
  //   }

  //   var result = await _userService.GetUserAsync(id);

  //   stopwatch.Stop();
  //   _logger.LogInformation("GET /api/v1/user completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

  //   if (result.StatusCode != 200)
  //   {
  //     // This handles unexpected cases where status is 200 but data is null
  //     _logger.LogWarning("GET /api/v1/user returned {StatusCode}: {Message}", result.StatusCode, result.Message);
  //     return StatusCode(500, new { status = 500, message = result.Message });
  //   }

  //   return StatusCode(result.StatusCode, new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message,
  //     data = result.Data ?? new object() // Ensure data is never null
  //   });
  // }

  // [HttpPost]
  // //[Authorize(Policy = "RequireOwnerAdminRole")]
  // public async Task<IActionResult> CreateUser(CreateUserDto entity)
  // {
  //   if (entity is null) return BadRequest(new { message = "Please enter your information." });
  //   var result = await _userService.AddUserAsync(entity);

  //   if (result.StatusCode != 200)
  //   {
  //     return StatusCode(result.StatusCode, new
  //     {
  //       status = result.StatusCode,
  //       message = result.Message
  //     });
  //   }

  //   return Ok(new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message,
  //     data = result.Data
  //   });
  // }

  // [HttpPost("bulk")]
  // //[Authorize(Policy = "RequireOwnerAdminRole")]
  // public async Task<IActionResult> CreateUsers(IEnumerable<CreateUserDto> entities)
  // {
  //   if (entities is null) return BadRequest(new { message = "Please enter your information." });
  //   var result = await _userService.AddUsersAsync(entities);

  //   return StatusCode(result.StatusCode, new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message
  //   });
  // }

  // [HttpDelete("{id}")]
  // [Authorize(Policy = "RequireOwnerAdminRole")]
  // public async Task<IActionResult> Delete(string id)
  // {
  //   var result = await _userService.DeleteUserAsync(id);
  //   if (result.StatusCode != 200) return StatusCode(result.StatusCode, new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message
  //   });

  //   return NoContent();
  // }

  // [HttpPatch("{id}")]
  // public async Task<IActionResult> Update(string id, UpdateUserDto entity)
  // {
  //   var result = await _userService.UpdateUserAsync(id, entity);
  //   if (result.StatusCode == 404) return NotFound(result);

  //   return NoContent();
  // }

  // [Authorize(Policy = "RequireOwnerAdminRole")]
  // [HttpPost("import")]
  // public async Task<IActionResult> ImportUsers(IFormFile file)
  // {

  //   if (file == null || file.Length == 0)
  //     return BadRequest("No file uploaded.");

  //   using var stream = file.OpenReadStream();
  //   var result = await _userService.AddUsersFromExcelAsync(stream);

  //   if (result.StatusCode == 200)
  //     return Ok(new
  //     {
  //       status = result.StatusCode,
  //       message = result.Message
  //     });

  //   return StatusCode(result.StatusCode, new
  //   {
  //     status = result.StatusCode,
  //     message = result.Message
  //   });
  // }

  // [Authorize(Policy = "RequireOwnerAdminRole")]
  // [HttpGet("export")]
  // public async Task<IActionResult> ExportUsersToExcel([FromQuery] int take = 20, [FromQuery] string? roleId = null)
  // {
  //   try
  //   {
  //     var fileBytes = await _userService.ExportUsersToExcelAsync(take, roleId);

  //     var fileName = $"users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
  //     return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
  //   }
  //   catch (Exception ex)
  //   {
  //     return StatusCode(500, new { message = "Internal server error while exporting users." });
  //     throw new Exception(ex.Message);
  //   }
  // }
}
