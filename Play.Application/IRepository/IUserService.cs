using Play.Application.DTOs;
using Play.Application.Model.User;
using Play.Domain.Entities;

namespace Play.API.IRepository
{
  public interface IUserService
  {
    Task<ResponseData<PaginatedResponse<UserDto>>> GetUsersAsync(QueryParameters parameters);
    Task<ResponseData<User>> GetUserAsync(string id);
    Task<ResponseData<User>> AddUserAsync(CreateUserRequest entity);
    Task AddUserAsync(UserDto dto);
    Task<ResponseData<List<User>>> AddUsersAsync(IEnumerable<CreateUserRequest> entities);
    Task<ResponseData<User>> UpdateUserAsync(string id, UpdateUserRequest entity);
    Task<ResponseData<User>> DeleteUserAsync(string id);
    Task<ResponseData<int>> AddUsersFromExcelAsync(Stream excelStream);
    Task<byte[]> ExportUsersToExcelAsync(int take = 20, string? roleId = null);
  }
}
