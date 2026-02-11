using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Infrastructure.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError; // Default to 500
            string title = "An error occurred.";
            string detail = "An internal server error occurred.";

            switch (exception)
            {
                case ValidationException validationException:
                    statusCode = HttpStatusCode.BadRequest;
                    title = "Validation Error";
                    detail = validationException.Message;
                    break;
                case NotFoundException notFoundException:
                    statusCode = HttpStatusCode.NotFound;
                    title = "Resource Not Found";
                    detail = notFoundException.Message;
                    break;
                case UnauthorizedAccessException unauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    title = "Unauthorized Access";
                    detail = unauthorizedAccessException.Message;
                    break;
                // Add more custom exceptions here if needed
                default:
                    // For unhandled exceptions, log details for debugging but return generic message to client
                    detail = "An unexpected internal server error occurred.";
                    break;
            }

            var problemDetails = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = (int)statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}