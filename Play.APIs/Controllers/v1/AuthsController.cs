using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Application.IRepository;
using Play.Infrastructure.Services;
using System.Diagnostics;

namespace Play.APIs.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthsController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly ILogger<AuthsController> _logger;

    public AuthsController(AuthService auth, ILogger<AuthsController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AuthenticateDto model)
    {
        try
        {
            var result = await _auth.LoginAsync(model);

            if (result.StatusCode != 200)
            {
                return StatusCode(result.StatusCode, new
                {
                    status = result.StatusCode,
                    message = result.Message
                });
            }

            return Ok(new
            {
                status = result.StatusCode,
                message = result.Message,
                token = result.Token,
                expireTime = result.TokenExpiredTime,
                refreshToken = result.RefreshToken,
                refreshTokenExpireTime = result.RefreshTokenExpiredTime
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the login request.");
            return StatusCode(500, new { status = 500, message = "Internal server error" });
        }
    }
}

// [Authorize]
// [HttpPost("refresh-token")]
// public IActionResult RefreshToken(TokenApiDto model)
// {
//   var result = _auth.RefreshToken(model);
//   return Ok(result);
// }

// [Authorize]
// [HttpPost("logout")]
// public IActionResult Logout()
// {
//   _auth.Logout();
//   return Ok(new
//   {
//     status = 200,
//     message = "Logged out"
//   });
// }


