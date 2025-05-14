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
      // Find the user by email
      var user = await _context.Users
          .Include(u => u.Role)
          .FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive && u.DeletedAt == null);

      if (user == null)
      {
        return new ResponseData<AuthResponse>
        {
          StatusCode = 400,
          Message = "Invalid email or password",
          Data = null
        };
      }

      // Verify the password using your existing method
      if (!VerifyPassword(user, model.Password))
      {
        return new ResponseData<AuthResponse>
        {
          StatusCode = 422,
          Message = "Invalid email or password",
          Data = null
        };
      }

      // Generate claims for the JWT token
      // Include enough information to identify the user without database lookups
      var claims = new List<Claim>
      {
          new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
          new Claim(ClaimTypes.Name, user.Name),
          new Claim(ClaimTypes.Email, user.Email),
          new Claim("Role", user.RoleId.ToString())
      };

      // Generate access token
      var accessToken = GenerateAccessToken(claims);
      var tokenExpiredTime = DateTime.UtcNow.AddHours(Convert.ToInt16(_config["AuthConfiguration:TokenExpiredTime"]));
      // Set cookies for both tokens
      SetJWTTokenCookie(accessToken);

      return ResponseData<AuthResponse>.Success(accessToken, tokenExpiredTime);
    }

    public ResponseData<AuthResponse> RefreshToken(string oldToken)
    {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.ASCII.GetBytes(_config["AuthConfiguration:Key"]);

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
        SetJWTTokenCookie(newAccessToken);

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
      }
    }

    public Task<ResponseData<UserDto>> Logout()
    {
      throw new NotImplementedException();
    }

    private void SetJWTTokenCookie(string token)
    {
      var cookieOptions = new CookieOptions
      {
        HttpOnly = true,
        Expires = DateTime.UtcNow.AddHours(Convert.ToDouble(_config["AuthConfiguration:TokenExpiredTime"])),
        Secure = true,
        SameSite = SameSiteMode.Strict
      };

      _httpContextAccessor.HttpContext?.Response.Cookies.Append("access_token", token, cookieOptions);
    }
  }
}
