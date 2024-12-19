using AccessControl.Application.Dtos.Common;
using Serilog;
using System.Net;

namespace AccessControl.Application.Middleware
{
    public class GlobalExceptionHandlerMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unhandled exception occurred: {ErrorMessage}", ex.Message);

                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            int statusCode = (int)HttpStatusCode.InternalServerError;
            string responseMessage = "An error occurred while processing your request.";
            string? details = null;

            switch (exception)
            {
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    responseMessage = "The requested resource was not found.";
                    break;
                case UnauthorizedAccessException:
                    statusCode = (int)HttpStatusCode.Unauthorized;
                    responseMessage = "Unauthorized access.";
                    break;
                case ArgumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    responseMessage = "A bad request was made.";
                    break;
                default:
                    if (env.IsDevelopment())
                    {
                        details = exception.Message;
                    }
                    break;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new GenericResponseDto<object>
            {
                Success = false,
                Message = responseMessage,
                Data = null,
                Errors = env.IsDevelopment() ? [exception.Message, exception.StackTrace ?? ""] : [details ?? ""]
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
