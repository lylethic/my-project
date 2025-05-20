using System;

namespace myproject.Middleware;

public class ErrorHandlingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<ErrorHandlingMiddleware> _logger;

  public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task Invoke(HttpContext context)
  {
    try
    {
      await _next(context); // Continue to next middleware
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unhandled exception occurred");
      context.Response.StatusCode = 500;
      await context.Response.WriteAsJsonAsync(new { status = 500, message = "Internal server error" });
    }
  }
}
