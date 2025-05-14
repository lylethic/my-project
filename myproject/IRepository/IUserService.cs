using myproject.DTOs;
using myproject.Entities;

namespace myproject.IRepository
{
  public interface IUserService
  {
    Task<ResponseData<User>> GetUsersAsync(bool? isActive);
    Task<ResponseData<User>> GetUserAsync(Guid id);
    Task<ResponseData<User>> AddUserAsync(CreateUserDto entity);
    Task<ResponseData<User>> UpdateUserAsync(Guid id, UpdateUserDto entity);
    Task<ResponseData<User>> DeleteUserAsync(Guid id);
  }
}
