using System;

namespace myproject.Middleware;

public class CookieJwtInjectorMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<CookieJwtInjectorMiddleware> _logger;

  public CookieJwtInjectorMiddleware(RequestDelegate next, ILogger<CookieJwtInjectorMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }
  public async Task InvokeAsync(HttpContext context)
  {
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
