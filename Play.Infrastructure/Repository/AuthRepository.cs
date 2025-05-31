using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Play.Application.DTOs;
using Play.Application.IRepository;
using Play.Domain.Entities;
using Play.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Play.Infrastructure.Repository
{
  public class AuthRepository : IAuth
  {
    private readonly ApiDbContext _context;
    private IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthRepository(ApiDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
      _context = context ?? throw new ArgumentNullException(nameof(context));
      _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
      _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private readonly PasswordHasher<User> _passwordHasher = new();

    // Separate method for hashing password
    private string HashPassword(User user, string plainPassword)
    {
      return _passwordHasher.HashPassword(user, plainPassword);
    }

    // Separate method for verifying password
    public bool VerifyPassword(User user, string inputPassword)
    {
      var result = _passwordHasher.VerifyHashedPassword(user, user.Password, inputPassword);
      return result == PasswordVerificationResult.Success;
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims)
    {
      var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AuthConfiguration:Key"]!));
      var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

      // Create a JWT
      var tokeOptions = new JwtSecurityToken(
        issuer: _config["AuthConfiguration:Issuer"],
        audience: _config["AuthConfiguration:Audience"],
        claims: claims,
        signingCredentials: signinCredentials,
        expires: DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]))
        );

      var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
      return tokenString;
    }

    public string GenerateRefreshToken()
    {
      var randomNumber = new byte[32];
      using (var rng = RandomNumberGenerator.Create())
      {
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
      }
    }

    public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
      var tokenValidationParameters = new TokenValidationParameters
      {
        ValidateAudience = false, // might be validate the audience and issuer
        ValidateIssuer = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AuthConfiguration:Key"]!)),
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

    public async Task<ResponseData<AuthResponse>> Login(AuthenticateDto model)
    {
      try
      {
        var user = await _context.Users
         .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive && u.DeletedAt == null);

        if (user == null)
        {
          return new ResponseData<AuthResponse>
          {
            StatusCode = 400,
            Message = "Invalid email or password"
          };
        }

        // Verify the password using your existing method
        if (!VerifyPassword(user, model.Password) || !Helpers.Utils.IsValidEmail(model.Email))
        {
          return new ResponseData<AuthResponse>
          {
            StatusCode = 422,
            Message = "Invalid email or password"
          };
        }

        // Generate claims for the JWT token
        // Include enough information to identify the user without database lookups
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FirstName),
            new Claim(ClaimTypes.Email, user.Email)
        };

        // Generate access token
        var accessToken = GenerateAccessToken(claims);
        var tokenExpiredTime = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]));
        // Set cookies for both tokens
        SetJWTTokenCookie("access_token", "access_token_expire", accessToken, tokenExpiredTime);

        var refreshToken = GenerateRefreshToken();
        var refreshTokenExpiredTime = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenRefreshExpiredTime"]));
        SetJWTTokenCookie("refresh_token", "refresh_token_expire", refreshToken, refreshTokenExpiredTime);

        return ResponseData<AuthResponse>.Success(accessToken, tokenExpiredTime, refreshToken, refreshTokenExpiredTime);
      }
      catch (Exception ex)
      {
        return ResponseData<AuthResponse>.Fail("Server error.", 500);
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
        var newAccessTokenExpire = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]));

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

      var expiredOptions = new CookieOptions
      {
        Expires = DateTime.UtcNow.AddDays(-1),
        Secure = true,
        HttpOnly = true,
        SameSite = SameSiteMode.Strict
      };

      context.Response.Cookies.Append("access_token", "", expiredOptions);

      var readableExpireOptions = new CookieOptions
      {
        Expires = DateTime.UtcNow.AddDays(-1),
        Secure = true,
        HttpOnly = false,
        SameSite = SameSiteMode.Strict
      };
      context.Response.Cookies.Append("access_token_expire", "", readableExpireOptions);

      return Task.FromResult(ResponseData<UserDto>.Success(200, "Logout successful"));
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

      // Set access_token
      _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieName, token, cookieOptions);

      // Set custom expire_time cookie (readable on client if needed)
      var readableExpireOptions = new CookieOptions
      {
        HttpOnly = false, // allow JavaScript to read it (optional)
        Expires = expireTime,
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      _httpContextAccessor.HttpContext?.Response.Cookies.Append(cookieNameExpire, expireTime.ToString("o"), readableExpireOptions);
    }
  }
}
