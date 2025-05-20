using Play.Application.DTOs;
using Play.Domain.Entities;

namespace Play.API.IRepository
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
