using System.Diagnostics;

namespace LogiTrack.Middlewares;

public class RequestTimingMiddleware
{
  private readonly RequestDelegate _next;
  private readonly ILogger<RequestTimingMiddleware> _logger;

  public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
  {
    _next = next;
    _logger = logger;
  }

  public async Task InvokeAsync(HttpContext context)
  {
    var stopwatch = Stopwatch.StartNew();

    await _next(context); // Call the next middleware

    stopwatch.Stop();
    var elapsedMs = stopwatch.ElapsedMilliseconds;

    var method = context.Request.Method;
    var path = context.Request.Path;

    _logger.LogInformation("Request {Method} {Path} took {ElapsedMilliseconds} ms",
        method, path, elapsedMs);
  }
}
