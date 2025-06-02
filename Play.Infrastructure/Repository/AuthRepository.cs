using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using ExcelDataReader.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Helpers;
using Play.Infrastructure.Common.Repositories;
using Play.Infrastructure.Common.Utilities;

namespace Play.Infrastructure.Repository
{
  public class AuthRepository : SimpleCrudRepositories<User, string>
  {
    private readonly IDbConnection _connection;
    private readonly EnvReader _envReader;
    private readonly IHttpContextAccessor _httpContextAccessor;
    public AuthRepository(IDbConnection connection, IHttpContextAccessor httpContextAccessor) : base(connection)
    {
      _connection = connection;
      _envReader = new EnvReader();
      _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ResponseData<AuthResponse>> Login(AuthenticateDto model)
    {
      try
      {
        // Query user by email
        var sql = $"""
                  SELECT role_id, email, first_name, password, last_name, is_active
                  FROM users
                  WHERE LOWER(email) = LOWER(@Email) AND is_active = true;
                  """;
        var user = await _connection
          .QuerySingleOrDefaultAsync<User>(sql, new { model.Email });

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
          return ResponseData<AuthResponse>.Fail("Invalid email or password", 400);

        if (!Utils.IsValidEmail(model.Email))
          return ResponseData<AuthResponse>.Fail("Invalid email format", 400);

        var roleSql = "SELECT name FROM roles WHERE id = @RoleId";
        var roleName = await _connection.QuerySingleOrDefaultAsync<string>(roleSql, new { user.RoleId });

        // Generate claims
        var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id),
                    new(ClaimTypes.Email, user.Email),
                    new(ClaimTypes.Role, roleName?? "user")
                };
        // Generate tokens
        var accessToken = GenerateAccessToken(claims);
        var tokenExpiredTime = DateTime.Now.AddHours(_envReader.GetInt("JWT_EXPIRY_HOURS"));
        var refreshToken = GenerateRefreshToken(claims);
        var refreshTokenExpiredTime = DateTime.Now.AddHours(_envReader.GetInt("JWT_REFRESH_EXPIRY_HOURS"));
        // Set cookies
        SetJWTTokenCookie(_envReader.GetString("ACCESSTOKEN_COOKIENAME"), _envReader.GetString("ACCESSTOKEN_EXPIRY_NAME"), accessToken, tokenExpiredTime);
        SetJWTTokenCookie(_envReader.GetString("REFRESHTOKEN_COOKIENAME"), _envReader.GetString("REFRESHTOKEN_EXPIRY_NAME"), refreshToken, refreshTokenExpiredTime);
        return ResponseData<AuthResponse>.Success(
          accessToken,
          tokenExpiredTime,
          refreshToken,
          refreshTokenExpiredTime
        );
      }
      catch (Exception ex)
      {
        return ResponseData<AuthResponse>.Fail($"Login failed: {ex.Message}", 500);
        throw new Exception(ex.Message);
      }
    }

    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_envReader.GetString("API_SECRET")));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var token = new JwtSecurityToken(
          issuer: _envReader.GetString("JWT_ISSUER"),
          audience: _envReader.GetString("JWT_AUDIENCE"),
          claims: claims,
          expires: DateTime.UtcNow.AddMinutes(_envReader.GetInt("JWT_EXPIRY_HOURS")),
          signingCredentials: creds
      );
      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(IEnumerable<Claim> claims)
    {
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_envReader.GetString("API_SECRET")));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var token = new JwtSecurityToken(
          issuer: _envReader.GetString("JWT_ISSUER"),
          audience: _envReader.GetString("JWT_AUDIENCE"),
          claims: claims,
          expires: DateTime.UtcNow.AddHours(_envReader.GetInt("JWT_REFRESH_EXPIRY_HOURS")),
          signingCredentials: creds
      );
      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetJWTTokenCookie(string cookieName, string cookieNameExpire, string token, DateTime expireTime)
    {
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Expires = expireTime,
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, cookieOptions);

      var readableExpireOptions = new CookieOptions
      {
        HttpOnly = false,
        Expires = expireTime,
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieNameExpire, expireTime.ToString("o"), readableExpireOptions);
    }
  }
}