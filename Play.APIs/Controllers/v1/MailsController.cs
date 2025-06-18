using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Play.Infrastructure.Common.Contracts;
using Play.Infrastructure.Common.Mail;
using Play.Infrastructure.Services;

namespace Play.APIs.Controllers.v1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mail")]
[ApiController]
public class MailsController : ControllerBase
{
    private readonly GmailService _gmailService;
    public MailsController(GmailService gmailService)
    {
        this._gmailService = gmailService;
    }

    [HttpPost]
    public async Task<IActionResult> SendEmail(SendEmailRequest request)
    {
        await _gmailService.SendEmailAsync(request);
        return Ok(new { status = 200, message = "Email sent successfully" });
    }

    [HttpGet("test-config")]
    public IActionResult TestConfig([FromServices] IOptions<GmailOptions> options)
    {
        var config = options.Value;
        return Ok(new
        {
            Host = config.Host,
            Port = config.Port,
            Email = config.Email,
            HasPassword = !string.IsNullOrEmpty(config.Password)
        });
    }
}
