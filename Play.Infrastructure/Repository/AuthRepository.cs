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
using Play.Application.Enums;
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

        public async Task<AuthResponse> Login(AuthenticateDto model)
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
                return new AuthResponse(AuthStatus.BadRequest, "Invalid email or password");

            if (!Utils.IsValidEmail(model.Email))
                return new AuthResponse(AuthStatus.BadRequest, "Invalid email format");

            var roleSql = "SELECT name FROM roles WHERE id = @RoleId";
            var roleName = await _connection.QuerySingleOrDefaultAsync<string>(roleSql, new { user.RoleId });

            // Generate claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName ?? string.Empty)
            };

            // Parse environment variables safely
            var expiryHoursStr = Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS");
            var refreshExpiryHoursStr = Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_HOURS");

            if (!double.TryParse(expiryHoursStr, out double expiryHours) ||
                !double.TryParse(refreshExpiryHoursStr, out double refreshExpiryHours))
            {
                return new AuthResponse(AuthStatus.InternalServerError, "Invalid token expiry configuration");
            }

            // Generate tokens
            var accessToken = GenerateAccessToken(claims);
            var tokenExpiredTime = DateTime.Now.AddHours(expiryHours);

            // var refreshToken = GenerateRefreshToken(claims);
            // var refreshTokenExpiredTime = DateTime.Now.AddHours(refreshExpiryHours);

            var accessTokenCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME");
            var accessTokenExpiryName = Environment.GetEnvironmentVariable("ACCESSTOKEN_EXPIRY_NAME");
            var refreshTokenCookieName = Environment.GetEnvironmentVariable("REFRESHTOKEN_COOKIENAME");
            var refreshTokenExpiryName = Environment.GetEnvironmentVariable("REFRESHTOKEN_EXPIRY_NAME");

            if (string.IsNullOrWhiteSpace(accessTokenCookieName) || string.IsNullOrWhiteSpace(accessTokenExpiryName) ||
                string.IsNullOrWhiteSpace(refreshTokenCookieName) || string.IsNullOrWhiteSpace(refreshTokenExpiryName))
            {
                return new AuthResponse(AuthStatus.InternalServerError, "Missing cookie environment variables");
            }

            SetJWTTokenCookie(accessTokenCookieName, accessTokenExpiryName, accessToken, tokenExpiredTime);
            // SetJWTTokenCookie(refreshTokenCookieName, refreshTokenExpiryName);

            return new AuthResponse(
                AuthStatus.Success,
                "Login successful",
                accessToken,
                tokenExpiredTime
            );
        }

        public AuthResponse RefreshToken(string token)
        {
            if (token is null)
                return new AuthResponse(AuthStatus.BadRequest, "Invalid request");

            var accessCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME");
            var accessExpiryName = Environment.GetEnvironmentVariable("ACCESSTOKEN_EXPIRY_NAME");

            // 2. Get user claims from expired access token
            var principal = GetPrincipalFromExpiredToken(token);
            if (principal == null)
                return new AuthResponse(AuthStatus.Unauthorized, "Invalid access token");

            // 3. Generate new access token
            var newAccessToken = GenerateAccessToken(principal.Claims);
            var newAccessTokenExpire = DateTime.Now.AddHours(Convert.ToInt16(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS")));

            // 4. Set new access token cookie
            SetJWTTokenCookie(accessCookieName, accessExpiryName, newAccessToken, newAccessTokenExpire);

            return new AuthResponse(AuthStatus.Success, "Refresh token successful", newAccessToken, newAccessTokenExpire);
        }

        public ResponseData<UserDto> Logout()
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                return ResponseData<UserDto>.Fail("HttpContext not found", AuthStatus.InternalServerError);
            }

            try
            {
                // Get cookie names from environment (same as login)
                var accessTokenCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_COOKIENAME") ?? "access_token";
                var accessTokenExpiryCookieName = Environment.GetEnvironmentVariable("ACCESSTOKEN_EXPIRY_NAME") ?? "access_token_expiry";
                var refreshTokenCookieName = Environment.GetEnvironmentVariable("REFRESHTOKEN_COOKIENAME") ?? "refresh_token";
                var refreshTokenExpiryCookieName = Environment.GetEnvironmentVariable("REFRESHTOKEN_EXPIRY_NAME") ?? "refresh_token_expiry";

                // Create expired cookie options for HttpOnly cookies (secure tokens)
                var secureExpiredOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(-1), // Set to past date to expire
                    Secure = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/" // Ensure we're clearing the right path
                };

                // Create expired cookie options for readable cookies (expiry times)
                var readableExpiredOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(-1),
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

                ClearAdditionalAuthCookies(context, readableExpiredOptions);

                return ResponseData<UserDto>.Success(AuthStatus.Success, "Logout successful");
            }
            catch (Exception ex)
            {
                return ResponseData<UserDto>.Fail($"Logout failed: {ex.Message}", AuthStatus.InternalServerError);
            }
        }

        // Reset password
        public async Task<ResponseData<string>> SendResetCode(string userEmail)
        {
            try
            {
                var isValidEmail = Utils.IsValidEmail(userEmail);
                if (isValidEmail == false)
                    return ResponseData<string>.Fail("Invalid email");

                var existingEmail = """
                    SELECT id, role_id, email, first_name, password, last_name, is_active
                    FROM users
                    WHERE LOWER(email) = LOWER(@Email) AND is_active = true;
                """;
                var user = await _connection
                    .QuerySingleOrDefaultAsync<User>(existingEmail, new { Email = userEmail });

                if (user == null)
                    return ResponseData<string>.Fail("User not found");
                else
                {
                    var resetCode = RandomNumber.GenerateRandomNumberList(6);

                    _memoryCache.Set($"ResetCode:{userEmail}", resetCode, TimeSpan.FromMinutes(5));

                    var subject = "Your Password Reset Code";

                    var body = $"""
                        <html>
                        <body>
                            <p>Hello <strong>{user.FirstName}</strong>,</p>
                            <p>Use the following code to reset your password:</p>
                            <h2 style="color: #007bff;">{resetCode}</h2>
                            <p>This code will expire in <strong>5 minutes</strong>.</p>
                            <p>Thanks,<br/>Loopy Team</p>
                        </body>
                        </html>
                    """;


                    var emailRequest = new SendEmailRequest(userEmail, subject, body);
                    await _mailService.SendEmailAsync(emailRequest);

                    return ResponseData<string>.Success(AuthStatus.Success, "Reset code sent successfully. Please check your email.");
                }
            }
            catch (Exception ex)
            {
                return ResponseData<string>.Fail($"Failed to send reset code: {ex.Message}");
            }
        }

        public async Task<ResponseData<string>> ConfirmResetPassword(ResetPasswordRequest request)
        {
            if (_memoryCache.TryGetValue($"ResetCode:{request.Email}", out string cachedCode) && cachedCode == request.Code)
            {
                // Code valid – update password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                string sql = "UPDATE users SET password = @password WHERE email = @email";
                int affected = await _connection.ExecuteAsync(sql, new { password = hashedPassword, email = request.Email });

                _memoryCache.Remove($"ResetCode:{request.Email}");

                if (affected > 0)
                    return ResponseData<string>.Success(AuthStatus.Success, "Password has been reset successfully.");
                else
                    return ResponseData<string>.Fail("Failed to update password.");
            }

            return ResponseData<string>.Fail("Invalid or expired reset code.");
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

        public static ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = Environment.GetEnvironmentVariable("API_SECRET")
                ?? throw new ArgumentNullException("API_SECRET is not set.");
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false, // might be validate the audience and issuer
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
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

        public static string? ValidateJwtToken(string? token)
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

        private static string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECRET")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                claims: claims,
                expires: DateTime.Now.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS"))),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string GenerateRefreshToken(IEnumerable<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("API_SECRET")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER"),
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
                claims: claims,
                expires: DateTime.Now.AddHours(double.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_HOURS"))),
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