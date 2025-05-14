using myproject.DTOs;
using System.Security.Claims;

namespace myproject.IRepository
{
  public interface IAuth
  {
    Task<ResponseData<AuthResponse>> Login(AuthenticateDto model);
    Task<ResponseData<UserDto>> Logout();
    ResponseData<AuthResponse> RefreshToken(string token);
    string GenerateAccessToken(IEnumerable<Claim> claims);

  }
}
