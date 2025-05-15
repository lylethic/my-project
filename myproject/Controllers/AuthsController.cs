using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using myproject.DTOs;
using myproject.IRepository;

namespace myproject.Controllers
{
  [Route("api/v1/auth")]
  [ApiController]
  public class AuthsController : ControllerBase
  {
    private readonly IAuth _auth;

    public AuthsController(IAuth auth)
    {
      this._auth = auth;
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(AuthenticateDto model)
    {
      if (!ModelState.IsValid)
        return BadRequest(new { status = 400, message = "Please enter your account!" });

      var result = await _auth.Login(model);
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
        expireTime = result.TokenExpiredTime
      });
    }

    [Authorize]
    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] string token)
    {
      var result = _auth.RefreshToken(token);
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
