using Play.Application.DTOs;
using System.Security.Claims;

namespace Play.Application.IRepository
{
  public interface IAuth
  {
    Task<ResponseData<AuthResponse>> Login(AuthenticateDto model);
    Task<ResponseData<UserDto>> Logout();
    ResponseData<AuthResponse> RefreshToken(TokenApiDto model);
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
  }
}
