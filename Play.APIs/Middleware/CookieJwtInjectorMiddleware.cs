using Play.Infrastructure.Common.Utilities;

namespace Play.APIs.Middleware;

public class CookieJwtInjectorMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<CookieJwtInjectorMiddleware> _logger;
  private readonly EnvReader _envReader;

  public CookieJwtInjectorMiddleware(RequestDelegate next, ILogger<CookieJwtInjectorMiddleware> logger)
  {
    _next = next;
    _logger = logger;
    _envReader = new EnvReader();
  }
  public async Task InvokeAsync(HttpContext context)
  {
    // retrieves the value of a cookie named access_token from the HTTP request's cookie collection
    var token = context.Request.Cookies["access_token"];

    if (!context.Request.Headers.ContainsKey("Authorization"))
    {
      if (!string.IsNullOrEmpty(token))
      {
        context.Request.Headers.Append("Authorization", $"Bearer {token}");
        _logger.LogInformation("JWT injected from access_token cookie.");
      }
      else
      {
        _logger.LogWarning("JWT not found in access_token cookie.");
      }
    }
    await _next(context);
  }
}
