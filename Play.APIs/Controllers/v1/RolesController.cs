using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.API.IRepository;
using Play.Application.DTOs;
using Play.Infrastructure.Repository;
using Play.Infrastructure.Services;

namespace Play.APIs.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
[ApiController]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly RoleService _roleService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(RoleService roleService, ILogger<RolesController> logger)
    {
        _logger = logger;
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request)
    {
        try
        {
            var response = await _roleService.GetPaginatedRolesAsync(request);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "RequireOwnerAdminRole")]
    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            await _roleService.CreateRoleAsync(request);
            return Created();
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Error craeting role.");
            return BadRequest(new { message = "An error occurred while craeting the role." });

        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoleById(string id)
    {
        var result = await _roleService.GetById(id);
        return Ok(result);
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateRole(string id, UpdateRoleRequest request)
    {
        try
        {
            var updatedRole = await _roleService.UpdateRoleAsync(id, request);
            return Ok(new { message = "Success", data = updatedRole });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error updating role with ID {RoleId}", id);
            return BadRequest(new { message = "An error occurred while updating the role." });

        }
    }

    [Authorize(Policy = "RequireOwnerRole")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        try
        {
            var deletedRole = await _roleService.DeleteRoleAsync(id);
            return Ok(new { message = "Success" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error deleting role with ID {RoleId}", id);
            return BadRequest(new { message = "An error occurred while deleting the role." });
        }
    }
}
