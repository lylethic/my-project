using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Play.Domain.Entities;
using System.Security.Claims;
using Play.Infrastructure.Common.Caching;

namespace Play.APIs.Controllers.v1;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IRedisCacheService _cache;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsersController(UserService userService, ILogger<UsersController> logger, IRedisCacheService redisCacheService, IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _logger = logger;
        _cache = redisCacheService;
        _httpContextAccessor = httpContextAccessor;
    }

    [Authorize(Policy = "RequireOwnerAdminRole")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
    {
        var userId = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // Create a more specific cache key that includes pagination parameters
        var cacheKey = $"users_{userId}_{request.IsActive}_{request.Page}";

        try
        {
            // Try to get from cache first
            var cachedResult = await _cache.GetDataAsync<PaginatedResponse<UserDto>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
                return Ok(cachedResult);
            }

            _logger.LogDebug("Cache miss for key: {CacheKey}. Fetching from database.", cacheKey);

            // Get data from service
            var result = await _userService.GetUsersAsync(request);

            if (result?.Data != null)
            {
                // Cache the result asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Use shorter expiration for paginated results
                        var expiration = TimeSpan.FromMinutes(2);
                        await _cache.SetDataAsync(cacheKey, result, expiration);
                        _logger.LogDebug("Successfully cached result for key: {CacheKey}", cacheKey);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cache result for key: {CacheKey}. Application will continue without caching.", cacheKey);
                    }
                });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Invalid argument for user request: {Message}", ex.Message);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while processing GetAll request for user: {UserId}", userId);
            return StatusCode(500, "An unexpected error occurred while processing your request");
        }
    }

    [Authorize(Policy = "RequireOwnerAdminRole")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _userService.GetById(id);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOwnerAdminRole")]
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

    [Authorize(Policy = "RequireOwnerAdminRole")]
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

    [Authorize(Policy = "RequireOwnerAdminRole")]
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

    [Authorize(Policy = "RequireOwnerAdminRole")]
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
    [Authorize(Policy = "RequireOwnerAdminRole")]
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
