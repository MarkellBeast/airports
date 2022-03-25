using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Threading.Tasks;
using Airports.WebHost.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Airports.WebHost.Middlewares.Error
{
    public class ExceptionHandlerMiddleware 
    {
        private const string DefaultServerErrorMessage = "Internal Server Error";

        private readonly RequestDelegate _next;

        private readonly Dictionary<Type, int> _statusCodeConversion = new Dictionary<Type, int>()
        {
            { typeof(ResourceNotFoundException), StatusCodes.Status404NotFound },
            { typeof(InvalidOperationException), StatusCodes.Status400BadRequest },
            { typeof(ArgumentNullException), StatusCodes.Status400BadRequest }
        };

        public ExceptionHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception e)
            {
                await HandleExceptionAsync(context, e);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            string message = null;

            if (_statusCodeConversion.TryGetValue(exception.GetType(), out int status))
            {
                statusCode = status;
                message = exception.Message;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsync(JsonConvert.SerializeObject(
                new ErrorDetails(statusCode, message ?? DefaultServerErrorMessage)));
        }
    }
}