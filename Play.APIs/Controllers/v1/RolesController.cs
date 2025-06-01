using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.API.IRepository;
using Play.Application.DTOs;
using Play.Infrastructure.Repository;
using Play.Infrastructure.Services;

namespace Play.APIs.Controllers
{
    [Route("api/roles")]
    [ApiController]
    //[Authorize]
    /// <summary> Key Points 
    /// Roles DELETE because it removes a resource.
    /// Returns 204 No Content on success.
    /// Returns 404 Not Found if the role does not exist.
    /// </summary>
    /// 
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

        // GET: api/v1/roles
        // [HttpGet]
        // //[Authorize(Policy = "RequireOwnerAdminRole")]
        // public async Task<IActionResult> GetRoles()
        // {
        //   var result = await _roleService.GetRolesAsync();
        //   if (result.StatusCode != 200)
        //     return StatusCode(
        //       result.StatusCode,
        //       new
        //       {
        //         status = result.StatusCode,
        //         message = result.Message
        //       });

        //   return Ok(new
        //   {
        //     status = result.StatusCode,
        //     message = result.Message,
        //     data = result.ListData
        //   });
        // }

        // // GET: api/v1/roles/{id}
        // [HttpGet("{id}")]
        // public async Task<IActionResult> GetRole(Guid id)
        // {
        //   var result = await _roleService.GetRoleAsync(id);
        //   if (result.StatusCode != 200)
        //     return NotFound(result);

        //   return StatusCode(result.StatusCode,
        //     new
        //     {
        //       status = result.StatusCode,
        //       message = result.Message,
        //       data = result.Data
        //     });
        // }

        // // POST: api/v1/roles
        // [HttpPost]
        // //[Authorize(Policy = "RequireOwnerAdminRole")]
        // public async Task<IActionResult> CreateRole(CreateRoleDto entity)
        // {
        //   if (entity is null) return BadRequest(new { message = "Please enter your role." });

        //   var result = await _roleService.AddRoleAsync(entity);
        //   if (result.StatusCode != 200) return StatusCode(result.StatusCode, new
        //   {
        //     status = result.StatusCode,
        //     message = result.Message
        //   });

        //   return StatusCode(result.StatusCode, new
        //   {
        //     status = result.StatusCode,
        //     message = result.Message
        //     ,
        //     data = result.Data
        //   });
        // }

        // [HttpPost("add")]
        // public async Task<IActionResult> Create(CreateRoleRequest request)
        // {
        //   await _roleService.CreateRoleAsync(request);
        //   return Ok();
        // }

        // // PUT: api/v1/roles/{id}
        // [HttpPut("{id}")]
        // [Authorize(Policy = "RequireOwnerAdminRole")]
        // public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleDto entity)
        // {
        //   var role = await _roleService.UpdateRoleAsync(id, entity);
        //   if (role.StatusCode == 404)
        //     return NotFound(role);

        //   return NoContent();
        // }

        // // DELETE: api/v1/roles/{id}
        // [HttpDelete("{id}")]
        // [Authorize(Policy = "RequireOwnerAdminRole")]
        // public async Task<IActionResult> DeleteRole(Guid id)
        // {
        //   var result = await _roleService.DeleteRoleAsync(id);
        //   if (result.StatusCode == 404) return NotFound(result);

        //   return NoContent();
        // }
    }
}
