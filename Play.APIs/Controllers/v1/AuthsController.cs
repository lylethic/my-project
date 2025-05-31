using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Application.DTOs;
using Play.Application.IRepository;
using System.Diagnostics;

namespace Play.APIs.Controllers
{
  [Route("api/auth")]
  [ApiController]
  public class AuthsController : ControllerBase
  {
    private readonly IAuth _auth;
    private readonly ILogger<AuthsController> _logger;

    public AuthsController(IAuth auth, ILogger<AuthsController> logger)
    {
      _auth = auth;
      _logger = logger;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AuthenticateDto model)
    {
      var stopwatch = Stopwatch.StartNew();
      _logger.LogInformation("POST /api/v1/auth called with model: {@Model}", model);
      _logger.LogInformation("Login attempt for user: {Email}", model.Email);

      if (!ModelState.IsValid)
        return BadRequest(new { status = 400, message = "Please enter your account!" });

      try
      {
        var result = await _auth.Login(model);
        stopwatch.Stop();
        _logger.LogInformation("POST /api/v1/auth completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

        // Log a unique request ID
        _logger.LogInformation("Request ID: {TraceId}", HttpContext.TraceIdentifier);

        if (result.StatusCode != 200)
        {
          _logger.LogWarning("POST /api/v1/auth returned {StatusCode}: {Message}", result.StatusCode, result.Message);
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
        stopwatch.Stop();
        _logger.LogError(ex, "Unhandled exception in POST /api/v1/auth after {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);
        return StatusCode(500, new { status = 500, message = "Internal server error" });
      }
    }

    [Authorize]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken(TokenApiDto model)
    {
      var result = _auth.RefreshToken(model);
      return Ok(result);
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
      _auth.Logout();
      return Ok(new
      {
        status = 200,
        message = "Logged out"
      });
    }
  }
}
