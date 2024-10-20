using System;
using System.Net;
using Newtonsoft.Json;
using WebApiApp.Models;

namespace WebApiApp.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
                _logger.LogError(ex, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = (int)HttpStatusCode.InternalServerError;
            string message;

            switch (exception)
            {
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    message = JsonConvert.SerializeObject(new { error = "Unauthorized access" });
                    break;
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = JsonConvert.SerializeObject(new { error = "Resource not found" });
                    break;
                case ArgumentException argEx:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = JsonConvert.SerializeObject(new { error = argEx.Message });
                    break;
                default:
                    // Логируем неожиданную ошибку и возвращаем сообщение
                    message = JsonConvert.SerializeObject(new { error = "An unexpected error occurred" });
                    break;
            }

            var errorResponse = new ErrorResponse
            {
                StatusCode = statusCode,
                Message = message
            };

            var result = JsonConvert.SerializeObject(errorResponse);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            return context.Response.WriteAsync(result);
        }
        
    }
}
