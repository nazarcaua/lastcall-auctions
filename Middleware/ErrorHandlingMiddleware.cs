using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace LastCallMotorAuctions.API.Middleware;

public class ErrorHandlingMiddleware
{

    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occured");

            if (!context.Response.HasStarted)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = HttpStatusCode.InternalServerError;
        var message = "An error has occured while processing your request.";

        // Handling specific exception types
        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                message = argEx.Message;
                break;

            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                message = "Unauthorized access.";
                break;

            default:
                statusCode = HttpStatusCode.InternalServerError;
                break;
        }

        object response;
        if (_env.IsDevelopment())
        {
            // Include detailed exception information in development for debugging
            response = new
            {
                message,
                statusCode = (int)statusCode,
                exception = exception.GetType().FullName,
                details = exception.ToString()
            };
        }
        else
        {
            response = new
            {
                message,
                statusCode = (int)statusCode
            };
        }

        context.Response.StatusCode = (int)statusCode;
        var jsonResponse = JsonSerializer.Serialize(response);
        return context.Response.WriteAsync(jsonResponse);
    }
}
