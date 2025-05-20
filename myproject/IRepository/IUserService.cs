using myproject.DTOs;
using myproject.Entities;

namespace myproject.IRepository
{
  public interface IUserService
  {
    Task<ResponseData<PaginatedResponse<UserDto>>> GetUsersAsync(QueryParameters parameters);
    Task<ResponseData<User>> GetUserAsync(Guid id);
    Task<ResponseData<User>> AddUserAsync(CreateUserDto entity);
    Task<ResponseData<List<User>>> AddUsersAsync(IEnumerable<CreateUserDto> entities);
    Task<ResponseData<User>> UpdateUserAsync(Guid id, UpdateUserDto entity);
    Task<ResponseData<User>> DeleteUserAsync(Guid id);
    Task<ResponseData<int>> AddUsersFromExcelAsync(Stream excelStream);
    Task<byte[]> ExportUsersToExcelAsync(int take = 20, Guid? roleId = null);
  }
}
