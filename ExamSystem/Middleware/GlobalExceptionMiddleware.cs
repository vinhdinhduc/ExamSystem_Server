using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using ExamSystem.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace ExamSystem.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response = exception switch
        {
            ValidationException validationEx => new
            {
                statusCode = 400,
                error = new ValidationError
                {
                    Details = validationEx.Errors.Select(e => new ValidationDetail
                    {
                        Field = e.PropertyName,
                        Message = e.ErrorMessage
                    }).ToList()
                },
                message = "Validation failed",
                data = (object?)null
            },
            KeyNotFoundException notFoundEx => new
            {
                statusCode = 404,
                error = new NotFoundError
                {
                    Resource = notFoundEx.Message
                },
                message = notFoundEx.Message,
                data = (object?)null
            },
            InvalidOperationException businessEx => new
            {
                statusCode = 400,
                error = new BusinessError
                {
                    Reason = businessEx.Message
                },
                message = businessEx.Message,
                data = (object?)null
            },
            UnauthorizedAccessException => new
            {
                statusCode = 401,
                error = new UnauthorizedError(),
                message = "Unauthorized access",
                data = (object?)null
            },
            _ => new
            {
                statusCode = 500,
                error = new InternalServerError
                {
                    Reason = exception.Message
                },
                message = "An internal server error occurred",
                data = (object?)null
            }
        };

        var statusCodeProperty = response.GetType().GetProperty("statusCode");
        var statusCode = statusCodeProperty?.GetValue(response) as int? ?? 500;
        context.Response.StatusCode = statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
