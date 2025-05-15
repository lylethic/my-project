using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using myproject.Data;
using myproject.DTOs;
using myproject.Entities;
using myproject.IRepository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace myproject.Repository
{
  public class AuthRepository : IAuth
  {
    private readonly ApiDbContext _context;
    private IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthRepository(ApiDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
      this._context = context;
      this._config = configuration;
      this._httpContextAccessor = httpContextAccessor;
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
        expires: DateTime.UtcNow.AddHours(Convert.ToDouble(_config["AuthConfiguration:TokenExpiredTime"]))
        );

      var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);

      return tokenString;
    }

    public async Task<ResponseData<AuthResponse>> Login(AuthenticateDto model)
    {
      try
      {
        var user = await _context.Users
         .Include(u => u.Role)
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
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name ?? user.RoleId.ToString())
        };

        // Generate access token
        var accessToken = GenerateAccessToken(claims);
        var tokenExpiredTime = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]));
        // Set cookies for both tokens
        SetJWTTokenCookie(accessToken, tokenExpiredTime);

        return ResponseData<AuthResponse>.Success(accessToken, tokenExpiredTime);
      }
      catch (Exception ex)
      {
        return ResponseData<AuthResponse>.Fail("Server error.", 500);
        throw new Exception(ex.Message);
      }
    }

    public ResponseData<AuthResponse> RefreshToken(string oldToken)
    {
      var tokenHandler = new JwtSecurityTokenHandler();

      var secretKey = _config["AuthConfiguration:Key"];
      if (string.IsNullOrEmpty(secretKey))
      {
        throw new InvalidOperationException("JWT secret key is missing in configuration.");
      }
      var key = Encoding.ASCII.GetBytes(secretKey);

      try
      {
        // Validate token and ignore expiration
        var principal = tokenHandler.ValidateToken(oldToken, new TokenValidationParameters
        {
          ValidateIssuer = false,
          ValidateAudience = false,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateLifetime = false // Ignore expired tokens
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        if (jwtToken == null)
        {
          return new ResponseData<AuthResponse>
          {
            StatusCode = 400,
            Message = "Invalid token",
            Data = null
          };
        }

        // Extract claims from the old token
        var claims = principal.Claims.ToList();

        // Generate a new access token
        var newAccessToken = GenerateAccessToken(claims);
        var newExpireTime = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]));

        // Set cookie or return token depending on your needs
        SetJWTTokenCookie(newAccessToken, newExpireTime);

        return ResponseData<AuthResponse>.Success(newAccessToken, newExpireTime);
      }
      catch (Exception ex)
      {
        return new ResponseData<AuthResponse>
        {
          StatusCode = 401,
          Message = "Token is invalid or tampered",
          Data = null
        };
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

    private void SetJWTTokenCookie(string token, DateTime expireTime)
    {
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Expires = expireTime,
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      // Set access_token
      _httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token", token, cookieOptions);

      // Set custom expire_time cookie (readable on client if needed)
      var readableExpireOptions = new CookieOptions
      {
        HttpOnly = false, // allow JavaScript to read it (optional)
        Expires = expireTime,
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      _httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token_expire", expireTime.ToString("o"), readableExpireOptions);
    }

  }
}
