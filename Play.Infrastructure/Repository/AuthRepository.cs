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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Play.Application.DTOs;
using Play.Domain.Entities;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Helpers;
using Play.Infrastructure.Common.Repositories;
using Play.Infrastructure.Common.Utilities;
using Play.Infrastructure.Services;

namespace Play.Infrastructure.Repository
{
  public class AuthRepository : SimpleCrudRepositories<User, string>
  {
    private readonly IDbConnection _connection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IMemoryCache _memoryCache;
    private readonly GmailService _mailService;

    public AuthRepository(IDbConnection connection, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, GmailService mailService) : base(connection)
    {
      this._connection = connection;
      this._httpContextAccessor = httpContextAccessor;
      this._memoryCache = memoryCache;
      this._mailService = mailService;
    }

    public async Task<ResponseData<AuthResponse>> Login(AuthenticateDto model)
    {
      try
      {
        // Query user by email
        var sql = $"""
                  SELECT id, role_id, email, first_name, password, last_name, is_active
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
                    new(ClaimTypes.Role, roleName ?? string.Empty)
                };
        // Generate tokens
        var accessToken = GenerateAccessToken(claims);
        var tokenExpiredTime = DateTime.Now.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS")));
        var refreshToken = GenerateRefreshToken(claims);
        var refreshTokenExpiredTime = DateTime.Now.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_HOURS")));
        // Set cookies
        SetJWTTokenCookie(Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME"), Environment.GetEnvironmentVariable("ACCESSTOKEN_EXPIRY_NAME"), accessToken, tokenExpiredTime);
        SetJWTTokenCookie(Environment.GetEnvironmentVariable("REFRESHTOKEN_COOKIENAME"), Environment.GetEnvironmentVariable("REFRESHTOKEN_EXPIRY_NAME"), refreshToken, refreshTokenExpiredTime);
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

    public ResponseData<AuthResponse> RefreshToken(TokenApiDto model)
    {
      if (model is null)
        return ResponseData<AuthResponse>.Fail("Invalid request", 400);

      try
      {
        string accessToken = model.AccessToken!;
        string refreshToken = model.RefreshToken!;

        // 1. Get refresh_token and its expire time from cookies
        var context = _httpContextAccessor.HttpContext;
        var cookieRefreshToken = context?.Request.Cookies["refresh_token"];
        var cookieRefreshTokenExpireStr = context?.Request.Cookies["refresh_token_expire"];

        if (cookieRefreshToken == null || cookieRefreshTokenExpireStr == null)
          return ResponseData<AuthResponse>.Fail("Refresh token not found", 401);

        if (cookieRefreshToken != refreshToken)
          return ResponseData<AuthResponse>.Fail("Refresh token mismatch", 403);

        if (!DateTime.TryParse(cookieRefreshTokenExpireStr, out var refreshExpireTime))
          return ResponseData<AuthResponse>.Fail("Invalid refresh token expiration", 400);

        if (refreshExpireTime < DateTime.UtcNow)
          return ResponseData<AuthResponse>.Fail("Refresh token expired", 401);

        // 2. Get user claims from expired access token
        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
          return ResponseData<AuthResponse>.Fail("Invalid access token", 401);

        // 3. Generate new access token
        var newAccessToken = GenerateAccessToken(principal.Claims);
        var newAccessTokenExpire = DateTime.UtcNow.AddHours(Convert.ToInt16(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS")));

        // 4. Set new access token cookie
        SetJWTTokenCookie("access_token", "access_token_expire", newAccessToken, newAccessTokenExpire);

        // reuse the old refresh token (still valid)

        return ResponseData<AuthResponse>.Success(newAccessToken, newAccessTokenExpire, refreshToken, refreshExpireTime);
      }
      catch (Exception ex)
      {
        return ResponseData<AuthResponse>.Fail("Server error: " + ex.Message, 500);
        throw new Exception(ex.Message);
      }
    }

    public Task<ResponseData<UserDto>> Logout()
    {
      var context = _httpContextAccessor.HttpContext;
      if (context == null)
      {
        return Task.FromResult(ResponseData<UserDto>.Fail("HttpContext not found", 500));
      }

      try
      {
        // Get cookie names from environment (same as login)
        var accessTokenCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME") ?? "access_token";
        var accessTokenExpiryCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_EXPIRY_NAME") ?? "access_token_expire";
        var refreshTokenCookieName = Environment.GetEnvironmentVariable("REFRESHTOKEN_COOKIENAME") ?? "refresh_token";
        var refreshTokenExpiryCookieName = Environment.GetEnvironmentVariable("REFRESHTOKEN_EXPIRY_NAME") ?? "refresh_token_expire";

        // Create expired cookie options for HttpOnly cookies (secure tokens)
        var secureExpiredOptions = new CookieOptions
        {
          Expires = DateTime.UtcNow.AddDays(-1), // Set to past date to expire
          Secure = true,
          HttpOnly = true,
          SameSite = SameSiteMode.Strict,
          Path = "/" // Ensure we're clearing the right path
        };

        // Create expired cookie options for readable cookies (expiry times)
        var readableExpiredOptions = new CookieOptions
        {
          Expires = DateTime.UtcNow.AddDays(-1),
          Secure = true,
          HttpOnly = false, // These can be read by JavaScript
          SameSite = SameSiteMode.Strict,
          Path = "/"
        };

        // Remove all token-related cookies
        context.Response.Cookies.Append(accessTokenCookieName, "", secureExpiredOptions);
        context.Response.Cookies.Append(accessTokenExpiryCookieName, "", readableExpiredOptions);
        context.Response.Cookies.Append(refreshTokenCookieName, "", secureExpiredOptions);
        context.Response.Cookies.Append(refreshTokenExpiryCookieName, "", readableExpiredOptions);

        // Optional: Clear any additional authentication cookies
        ClearAdditionalAuthCookies(context, readableExpiredOptions);

        // Optional: Add the token to a blacklist (for enhanced security)
        // await BlacklistCurrentToken(context);

        return Task.FromResult(ResponseData<UserDto>.Success(200, "Logout successful"));
      }
      catch (Exception ex)
      {
        return Task.FromResult(ResponseData<UserDto>.Fail($"Logout failed: {ex.Message}", 500));
      }
    }

    // Reset password

    public async Task<string> SendResetCode(string userEmail)
    {
      try
      {
        // Step 1: Generate random code
        var resetCode = RandomNumber.GenerateRandomNumberList(6);

        // Step 2: Cache the code for later verification
        _memoryCache.Set($"ResetCode:{userEmail}", resetCode, TimeSpan.FromMinutes(5));

        // Step 3: Email the code to user
        var subject = "Your Password Reset Code";
        var body = $"Hello,\n\nUse the following code to reset your password: {resetCode}\n\nThis code will expire in 5 minutes.\n\nThanks,\nLoopy Team!";
        var emailRequest = new SendEmailRequest(userEmail, subject, body);
        await _mailService.SendEmailAsync(emailRequest);

        return "Reset code sent successfully. Please check your email.";
      }
      catch (Exception ex)
      {
        throw new Exception($"Failed to send reset code: {ex.Message}");
      }
    }

    public async Task<string> ConfirmResetPassword(ResetPasswordRequest request)
    {
      try
      {
        if (_memoryCache.TryGetValue($"ResetCode:{request.Email}", out string cachedCode) && cachedCode == request.Code)
        {
          // Code valid – update password
          string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
          string sql = "UPDATE users SET password = @password WHERE email = @email";
          int affected = await _connection.ExecuteAsync(sql, new { password = hashedPassword, email = request.Email });

          _memoryCache.Remove($"ResetCode:{request.Email}");

          if (affected > 0)
            return "Password has been reset successfully.";
          else
            throw new Exception("Failed to update password.");
        }

        throw new Exception("Invalid or expired reset code.");
      }
      catch (Exception ex)
      {
        throw new Exception($"Reset failed: {ex.Message}");
      }
    }

    // HELPER METHOD: Clear additional authentication cookies
    private static void ClearAdditionalAuthCookies(HttpContext context, CookieOptions expiredOptions)
    {
      // Clear any other auth-related cookies your app might use
      var additionalCookiesToClear = new[]
      {
        ".AspNetCore.Identity.Application", // If using Identity
        "user_preferences",                 // Custom user data
        "session_id",                      // Session cookies
        "remember_me"                      // Remember me functionality
    };

      foreach (var cookieName in additionalCookiesToClear)
      {
        if (context.Request.Cookies.ContainsKey(cookieName))
        {
          context.Response.Cookies.Append(cookieName, "", expiredOptions);
        }
      }
    }
    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateAudience = false, // might be validate the audience and issuer
        ValidateIssuer = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECRET"))),
        ValidateLifetime = false // here saying that we don't care about the token's expiration date
      };
      var tokenHandler = new JwtSecurityTokenHandler();
      SecurityToken securityToken;
      var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
      var jwtSecurityToken = securityToken as JwtSecurityToken;
      if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        throw new SecurityTokenException("Invalid token");
      return principal;
    }

    public string? ValidateJwtToken(string? token)
    {
      if (token == null)
        return null;

      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("API_SECRET"));
      try
      {
        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = false,
          ValidateAudience = false,
          // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
          ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        var userId = jwtToken.Claims.First(x => x.Type == "id").Value;

        // return user id from JWT token if validation successful
        return userId;
      }
      catch
      {
        // return null if validation fails
        return null;
      }
    }

    private string GenerateAccessToken(IEnumerable<Claim> claims)
    {
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECRET")));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var token = new JwtSecurityToken(
          issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
          audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
          claims: claims,
          expires: DateTime.UtcNow.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS"))),
          signingCredentials: creds
      );
      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(IEnumerable<Claim> claims)
    {
      var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECRET")));
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
      var token = new JwtSecurityToken(
          issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
          audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
          claims: claims,
          expires: DateTime.UtcNow.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_HOURS"))),
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