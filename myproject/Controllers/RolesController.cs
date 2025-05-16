using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers
{
  [Route("api/v1/roles")]
  [ApiController]
  [Authorize]
  /// <summary> Key Points 
  /// Roles DELETE because it removes a resource.
  /// Returns 204 No Content on success.
  /// Returns 404 Not Found if the role does not exist.
  /// </summary>
  /// 
  public class RolesController : ControllerBase
  {
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
      this._roleService = roleService;
    }

    // GET: api/v1/roles
    [HttpGet]
    [Authorize(Policy = "RequireOwnerAdminRole")]
    public async Task<IActionResult> GetRoles()
    {
      var result = await _roleService.GetRolesAsync();
      if (result.StatusCode != 200)
        return StatusCode(
          result.StatusCode,
          new
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

    // GET: api/v1/roles/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
      var result = await _roleService.GetRoleAsync(id);
      if (result.StatusCode != 200)
        return NotFound(result);

      return StatusCode(result.StatusCode,
        new
        {
          status = result.StatusCode,
          message = result.Message,
          data = result.Data
        });
    }

    // POST: api/v1/roles
    [HttpPost]
    [Authorize(Policy = "RequireOwnerAdminRole")]
    public async Task<IActionResult> CreateRole(CreateRoleDto entity)
    {
      if (entity is null) return BadRequest(new { message = "Please enter your role." });

      var result = await _roleService.AddRoleAsync(entity);
      if (result.StatusCode != 200) return StatusCode(result.StatusCode, new
      {
        status = result.StatusCode,
        message = result.Message
      });

      return StatusCode(result.StatusCode, new
      {
        status = result.StatusCode,
        message = result.Message
        ,
        data = result.Data
      });
    }

    // PUT: api/v1/roles/{id}
    [HttpPut("{id}")]
    [Authorize(Policy = "RequireOwnerAdminRole")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleDto entity)
    {
      var role = await _roleService.UpdateRoleAsync(id, entity);
      if (role.StatusCode == 404)
        return NotFound(role);

      return NoContent();
    }

    // DELETE: api/v1/roles/{id}
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireOwnerAdminRole")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
      var result = await _roleService.DeleteRoleAsync(id);
      if (result.StatusCode == 404) return NotFound(result);

      return NoContent();
    }
  }
}
