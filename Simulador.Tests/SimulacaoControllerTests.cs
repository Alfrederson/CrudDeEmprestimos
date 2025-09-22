
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Simulador.Core.Models;
using Simulador.Web.Controllers;
using Xunit.Abstractions;

namespace Simulador.Tests
{
    public class SimulacaoControllerTests(ITestOutputHelper log)
    {
        [Fact]
        public async Task SimulacaoComProdutoInexistenteDeveRetornar404() =>
            await Util.TesteController<SimulacaoController>(async (_db, _logger) =>
            {
                var controller = new SimulacaoController(_db.Object, _logger);
                var result = Assert.IsType<NotFoundResult>(await controller.Simular(new SimulacaoController.RequisicaoSimulacaoBody
                {
                    IdProduto = 3,
                    PrazoMeses = 10,
                    ValorSolicitado = 200
                }));
                Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            });
        [Fact]
        public async Task SimulacaoComParametrosInvalidosDeveRetornar400() =>
            await Util.TesteController<SimulacaoController>(async (_db, _logger) =>
            {
                var db = _db.Object;
                var valido = new Produto { Nome = "Nome valido", PrazoMaximoMeses = 32, TaxaJurosAnual = 18.0m };
                db.Produtos.Add(valido);
                await db.SaveChangesAsync();

                var controller = new SimulacaoController(_db.Object, _logger);
                var result = Assert.IsType<ObjectResult>(await controller.Simular(new SimulacaoController.RequisicaoSimulacaoBody
                {
                    IdProduto = valido.Id,
                    PrazoMeses = 16,
                    ValorSolicitado = -10000.0m
                }));
                Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
                var details = Assert.IsType<ProblemDetails>(result.Value);

                Assert.Contains(Simulacao.E_VALOR_SOLICITADO_INVALIDO, details.Detail);
            });

        [Fact]
        public async Task SimulacaoComParametrosValidosDeveRetornar200() =>
            await Util.TesteController<SimulacaoController>(async (_db, _logger) =>
            {
                var db = _db.Object;
                var valido = new Produto { Nome = "Nome valido", PrazoMaximoMeses = 32, TaxaJurosAnual = 18.0m };
                db.Produtos.Add(valido);
                await db.SaveChangesAsync();

                var controller = new SimulacaoController(_db.Object, _logger);
                var result = Assert.IsType<OkObjectResult>(await controller.Simular(new SimulacaoController.RequisicaoSimulacaoBody
                {
                    IdProduto = valido.Id,
                    PrazoMeses = 16,
                    ValorSolicitado = 10000.0m
                }));
                Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
                var simulacao = Assert.IsType<Simulacao>(result.Value);

                Assert.Equal(16, simulacao.PrazoMeses);
                Assert.Equal(10000.0m, simulacao.ValorSolicitado);

                log.WriteLine(Util.EncodeJson(simulacao));
            });

    }
}