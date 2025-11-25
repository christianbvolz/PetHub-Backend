using System.Net;
using System.Text.Json;

namespace pethub.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger
)
{
    // The "next" delegate is the reference to the next middleware in the pipeline
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Try to pass the request to the next step (e.g., the Controller)
            await next(context);
        }
        catch (Exception ex)
        {
            // If ANY error happens down the line, we catch it here
            logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        // Default to 500 Internal Server Error
        var response = new
        {
            message = "An internal server error occurred.",
            details = exception.Message,
        };
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Custom logic: You can handle specific errors differently
        // Example: If it's a specific "NotFoundException", return 404.

        // Serialize the error response to JSON
        var jsonResponse = JsonSerializer.Serialize(response);

        return context.Response.WriteAsync(jsonResponse);
    }
}
