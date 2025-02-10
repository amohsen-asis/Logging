using System.Net;
using System.Text.Json;
using Logging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace Logging.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionMiddleware> logger, 
            IHostEnvironment env)
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Default to internal server error
            var statusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                TraceId = context.TraceIdentifier
            };

            // Prepare logging properties
            var logProperties = new Dictionary<string, object>
            {
                ["TraceId"] = context.TraceIdentifier,
                ["Path"] = context.Request.Path,
                ["HttpMethod"] = context.Request.Method,
                ["ClientIP"] = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
            };

            // Customize error handling based on exception type
            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Message = "Unauthorized access";
                    logProperties["ExceptionType"] = "Unauthorized";
                    break;

                case ArgumentException argEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Message = "Invalid argument provided";
                    logProperties["ExceptionType"] = "ArgumentError";
                    logProperties["ParamName"] = argEx.ParamName;
                    break;

                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Message = "Resource not found";
                    logProperties["ExceptionType"] = "NotFound";
                    break;

                case Microsoft.EntityFrameworkCore.DbUpdateException:
                    statusCode = (int)HttpStatusCode.Conflict;
                    errorResponse.Message = "Database update error";
                    logProperties["ExceptionType"] = "DatabaseError";
                    break;

                default:
                    errorResponse.Message = "An unexpected error occurred";
                    logProperties["ExceptionType"] = "UnexpectedError";
                    break;
            }

            // Add exception details in development
            if (_env.IsDevelopment())
            {
                errorResponse.Details = exception.ToString();
                logProperties["FullException"] = exception.ToString();
            }

            // Structured logging with semantic properties
            _logger.LogError(
                exception,
                "Exception Occurred: {ExceptionMessage} | " +
                "Type: {ExceptionType} | " +
                "Path: {RequestPath} | " +
                "Method: {HttpMethod} | " +
                "Client IP: {ClientIP} | " +
                "Trace ID: {TraceId}",
                errorResponse.Message,
                logProperties["ExceptionType"],
                logProperties["Path"],
                logProperties["HttpMethod"],
                logProperties["ClientIP"],
                context.TraceIdentifier
            );

            // Set response details
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            // Write the error response
            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    // Extension method for easy middleware registration
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}
