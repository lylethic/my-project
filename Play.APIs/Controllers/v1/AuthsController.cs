using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Infrastructure.Common.Abstracts;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Services;

namespace Play.APIs.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[ApiController]
public class AuthsController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<AuthsController> _logger;

    public AuthsController(AuthService auth, ILogger<AuthsController> logger)
    {
        _authService = auth;
        _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AuthenticateDto model)
    {
        try
        {
            var result = await _authService.LoginAsync(model);

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


    [Authorize]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken(TokenApiDto model)
    {
        var result = _authService.RefreshTokenAsync(model);
        return Ok(result);
    }
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _authService.LogoutAsync();
        return Ok(new
        {
            status = 200,
            message = "Logged out"
        });
    }

    [Authorize]
    [HttpPost("send-reset-code")]
    public async Task<IActionResult> SendResetCode([FromBody] string email)
    {
        var result = await _authService.SendResetCodeAsync(email);
        return Ok(new { status = 200, message = result });
    }

    [HttpPost("confirm-reset")]
    public async Task<IActionResult> ConfirmResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ConfirmResetPasswordAsync(request);

        return Ok(new { status = 200, message = result });
    }
}