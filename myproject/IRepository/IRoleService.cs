using myproject.DTOs;
using myproject.Entities;

namespace myproject.IRepository
{
  public interface IRoleService
  {
    Task<ResponseData<Role>> AddRoleAsync(CreateRoleDto entity);
    Task<ResponseData<Role>> UpdateRoleAsync(Guid id, UpdateRoleDto entity);
    Task<ResponseData<Role>> DeleteRoleAsync(Guid id);
    Task<ResponseData<RoleDto>> GetRoleAsync(Guid id);
    Task<ResponseData<RoleDto>> GetRolesAsync();
  }
}
