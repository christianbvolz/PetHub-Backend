using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace PetHub.API.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IHostEnvironment environment
)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Don't log OperationCanceledException (client disconnected)
            if (ex is OperationCanceledException)
            {
                context.Response.StatusCode = 499; // Client Closed Request
                return;
            }

            logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";

        var problemDetails = CreateProblemDetails(context, exception);

        context.Response.StatusCode =
            problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
    {
        // Determine status code and title based on exception type
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            ArgumentException or ArgumentNullException => (
                HttpStatusCode.BadRequest,
                "Invalid argument"
            ),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied"),
            InvalidOperationException => (HttpStatusCode.Conflict, "Invalid operation"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred"),
        };

        var problemDetails = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{(int)statusCode}",
            Instance = context.Request.Path,
        };

        // Only expose exception details in Development environment
        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }
        else
        {
            // Production: generic message
            problemDetails.Detail =
                statusCode == HttpStatusCode.InternalServerError
                    ? "An internal server error occurred. Please try again later."
                    : exception.Message;
        }

        return problemDetails;
    }
}
