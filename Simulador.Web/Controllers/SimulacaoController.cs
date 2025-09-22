using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Simulador.Core.Data;
using Simulador.Core.Exceptions;
using Simulador.Core.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Simulador.Web.Controllers
{
    [ApiController]
    [Route("/simulacao")]
    [SwaggerTag("Paradinha para fazer a simulação")]

    public class SimulacaoController(SimuladorDbContext ctx, ILogger<SimulacaoController> logger) : ControllerBase
    {
        private readonly SimuladorDbContext _ctx = ctx;
        private readonly ILogger<SimulacaoController> _logger = logger;

        public record RequisicaoSimulacaoBody
        {
            [SwaggerSchema("Id do produto")]
            [JsonPropertyName("idProduto")]
            public int IdProduto { get; set; }

            [SwaggerSchema("Valor do empréstimo")]
            [JsonPropertyName("valorSolicitado")]
            public decimal ValorSolicitado { get; set; }

            [SwaggerSchema("Prazo em meses")]
            [JsonPropertyName("prazoMeses")]
            public int PrazoMeses { get; set; }
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Fazer uma simulação de empréstimo")]
        [SwaggerResponse(StatusCodes.Status200OK, "Se a simulação foi bem sucedida", typeof(Simulacao))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Se os produtos são inválidos")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Se ela é baseada em um produto que não existe")]
        public async Task<IActionResult> Simular(RequisicaoSimulacaoBody parametros)
        {
            try
            {
                if (await _ctx.Produtos.FindAsync(parametros.IdProduto) is Produto produto)
                {
                    var simulacao = new Simulacao
                    {
                        Produto = produto,
                        PrazoMeses = parametros.PrazoMeses,
                        ValorSolicitado = parametros.ValorSolicitado
                    };
                    simulacao.CalcularPagamentos();
                    return Ok(simulacao);
                }
                return NotFound();
            }
            catch (SimuladorException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
        }


    }
}