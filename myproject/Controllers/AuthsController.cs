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
      var auth = await _auth.Login(model);
      return Ok(auth);
    }

    [HttpPost("refresh-token")]
    public IActionResult RefreshToken([FromBody] string token)
    {
      var result = _auth.RefreshToken(token);
      return Ok(result);
    }

  }
}
