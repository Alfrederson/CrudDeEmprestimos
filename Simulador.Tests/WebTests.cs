using System.Data;
using System.Reflection;
using Castle.Core.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Simulador.Core.Data;
using Simulador.Core.Models;
using Simulador.Web.Controllers;
using Simulador.Web.Middleware;
using SQLitePCL;
using Xunit.Abstractions;

namespace Simulador.Tests
{
    public class WebTests(ITestOutputHelper log)
    {

        [Fact]
        public async Task FiltroExceptionDeveRetornarMensagemDeErro()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            var mid = new ExceptionToResponse(
                _ => throw new Exception("Exception qualquer"),
                Util.LoggerDeTeste<ExceptionToResponse>()
            );
            await mid.Invoke(ctx);
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            Assert.Equal(StatusCodes.Status500InternalServerError, ctx.Response.StatusCode);
            var reader = new StreamReader(ctx.Response.Body);
            var resposta = reader.ReadToEnd();
            Assert.Contains("Exception qualquer",resposta);
        }
    }    
}
