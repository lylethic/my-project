using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Application.IRepository;
using Play.Infrastructure.Services;
using System.Diagnostics;

namespace Play.APIs.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthsController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly UserService _userService;
        private readonly ILogger<AuthsController> _logger;

        public AuthsController(AuthService auth, ILogger<AuthsController> logger, UserService userService)
        {
            _auth = auth;
            _logger = logger;
            _userService = userService;
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

        // 1. Register - Send OTP
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
        {
            var result = await _userService.RegisterAsync(request);
            return StatusCode(result.StatusCode, result);
        }

        // 2. Verify OTP - Create User
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var result = await _userService.VerifyOtpAndCreateUserAsync(request.Email, request.Otp);
            return StatusCode(result.StatusCode, result);
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
}

