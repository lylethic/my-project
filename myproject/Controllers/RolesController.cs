using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers
{
  [Route("api/v1/roles")]
  [ApiController]
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
    public async Task<IActionResult> GetRoles()
    {
      var role = await _roleService.GetRolesAsync();
      if (role.StatusCode != 200)
        return StatusCode(
          role.StatusCode,
          new
          {
            status = role.StatusCode,
            message = role.Message
          });

      return Ok(new
      {
        message = role.Message,
        data = role.ListData
      });
    }

    // GET: api/v1/roles/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRole(Guid id)
    {
      var role = await _roleService.GetRoleAsync(id);
      if (role.StatusCode != 200)
        return NotFound(role);

      return StatusCode(role.StatusCode,
        new
        {
          message = role.Message,
          data = role.Data
        });
    }

    // POST: api/v1/roles
    [HttpPost]
    public async Task<IActionResult> CreateRole(CreateRoleDto entity)
    {
      if (entity is null) return BadRequest(new { message = "Please enter your role." });

      var role = await _roleService.AddRoleAsync(entity);
      if (role.StatusCode != 200) return StatusCode(role.StatusCode, new
      {
        status = role.StatusCode,
        message = role.Message
      });

      return StatusCode(role.StatusCode, new
      {
        status = role.StatusCode,
        data = role.Data
      });
    }

    // PUT: api/v1/roles/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleDto entity)
    {
      var role = await _roleService.UpdateRoleAsync(id, entity);
      if (role.StatusCode == 404)
        return NotFound(role);

      return NoContent();
    }

    // DELETE: api/v1/roles/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(Guid id)
    {
      var product = await _roleService.DeleteRoleAsync(id);
      if (product.StatusCode == 404) return NotFound(product);

      return NoContent();
    }
  }
}
