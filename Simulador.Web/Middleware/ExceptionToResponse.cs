using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Simulador.Core.Exceptions;

namespace Simulador.Web.Middleware
{
    public class ExceptionToResponse(RequestDelegate next, ILogger<ExceptionToResponse> logger)
    {
        static readonly Dictionary<Type, int> ExceptionToCode = new()
        {
            { typeof(SimuladorException), StatusCodes.Status400BadRequest }
        };
        
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Algo deu errado processando a requisição");
                await HandleException(context, ex);
            }
        }
        private static Task HandleException(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = ExceptionToCode.GetValueOrDefault(exception.GetType(),StatusCodes.Status500InternalServerError);        
            return context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = exception.Message,
            }));
        }
    }
}