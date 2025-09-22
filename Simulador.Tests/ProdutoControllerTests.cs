

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Simulador.Core.Models;
using Simulador.Web.Controllers;
using Xunit.Abstractions;

namespace Simulador.Tests
{
    public class ProdutoControllerTests(ITestOutputHelper log)
    {
        [Fact]
        public async Task IndexDeveRetornarListaVaziaComBancoZerado()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                var controller = new ProdutoController(db, _logger);
                var result = await controller.Index();
                var ok_result = Assert.IsType<OkObjectResult>(result);
                var lista = Assert.IsAssignableFrom<List<Produto>>(ok_result.Value);
                Assert.Empty(lista);
            });
        }

        [Fact]
        public async Task ProdutoInexistenteDeveRetornarNotFound()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                var controller = new ProdutoController(db, _logger);
                var result = await controller.GetOne(3);
                Assert.IsType<NotFoundResult>(result);
            });
        }

        [Fact]
        public async Task ProdutoExistenteDeveRetornarOProduto()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;
                var criado = new Produto { Nome = "Nome valido", PrazoMaximoMeses = 32, TaxaJurosAnual = 18.0m };

                db.Produtos.Add(criado);
                await db.SaveChangesAsync();

                var controller = new ProdutoController(db, _logger);
                var result = await controller.GetOne(1);

                var ok_result = Assert.IsType<OkObjectResult>(result);
                var lido = Assert.IsAssignableFrom<Produto>(ok_result.Value);

                Assert.Equivalent(criado, lido);
                log.WriteLine(Util.EncodeJson(lido));
            });
        }

        [Fact]
        public async Task CriarUmProdutoValidoDeveCriarItemNoBancoDeDados()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;
                var controller = new ProdutoController(db, _logger);
                var result = Assert.IsType<CreatedAtActionResult>(
                    await controller.Create(
                        new ProdutoController.ProdutoBody
                        {
                            Nome = "Nome valido",
                            TaxaJurosAnual = 18.0m,
                            PrazoMaximoMeses = 19
                        }
                    )
                );
                Assert.NotEmpty(db.Produtos);
                Assert.Single(db.Produtos,
                    p => p.Nome == "Nome valido" && p.TaxaJurosAnual == 18.0m && p.PrazoMaximoMeses == 19
                );
                log.WriteLine(result.ToString());
            });
        }

        [Fact]
        public async Task CriarUmProdutoInvalidoDeveRetornarStatus400()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var controller = new ProdutoController(_db.Object, _logger);
                var result = await controller.Create(
                    new ProdutoController.ProdutoBody
                    {
                        Nome = "nome valido",
                        TaxaJurosAnual = 4.0m,
                        PrazoMaximoMeses = -4
                    }
                );
                var obj = Assert.IsType<ObjectResult>(result);
                var details = Assert.IsAssignableFrom<ProblemDetails>(obj.Value);
                Assert.Contains(Produto.E_PRAZO_INVALIDO, details.Detail);
            });
        }

        [Fact]
        public async Task CriarProdutoDeveDar500QuandoDaOutraException() =>
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                _db.Setup(
                    c => c.SaveChangesAsync(It.IsAny<CancellationToken>())
                ).ThrowsAsync(new Exception("Erro simulado"));
                var controller = new ProdutoController(db, _logger);
                var result = Assert.IsType<ObjectResult>(
                    await controller.Create(new ProdutoController.ProdutoBody
                    {
                        Nome = "Nome valido",
                        PrazoMaximoMeses = 33,
                        TaxaJurosAnual = 22
                    })
                );
                Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            });


        [Fact]
        public async Task DeletarUmProdutoEistenteDeveRetornarNoContent()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                db.Produtos.Add(new Produto { Nome = "Qualquer coisa", TaxaJurosAnual = 10.0m, PrazoMaximoMeses = 20 });
                await db.SaveChangesAsync();

                var controller = new ProdutoController(db, _logger);
                var result = await controller.Delete(1);
                Assert.IsType<NoContentResult>(result);
            });
        }


        [Fact]
        public async Task DeletarUmProdutoInexistenteDeveRetornar404()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                var controller = new ProdutoController(db, _logger);
                var result = await controller.Delete(1);
                Assert.IsType<NotFoundResult>(result);
            });
        }

        [Fact]
        public async Task DeletarUmProdutoDeveRetornarStatus500SeDerErroDeBancoDeDados()
        {
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                // não consegui fazer mock do dbset dentro do mock do db
                db.Produtos.Add(new Produto { });
                await db.SaveChangesAsync();

                _db.Setup(
                    c => c.SaveChangesAsync(It.IsAny<CancellationToken>())
                ).ThrowsAsync(new DbUpdateException("Erro simulado"));

                var controller = new ProdutoController(db, _logger);
                var result = Assert.IsType<ObjectResult>(await controller.Delete(1));
                Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            });
        }

        [Fact]
        public async Task ModificarUmProdutoDeveModificarOProduto() =>
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;
                var criado = new Produto { Nome = "Produto Cobaia", TaxaJurosAnual = 20.0m, PrazoMaximoMeses = 36 };
                db.Produtos.Add(criado);
                await db.SaveChangesAsync();
                db.Produtos.Entry(criado).State = EntityState.Detached;

                var controller = new ProdutoController(db, _logger);
                var result = Assert.IsType<NoContentResult>(
                    await controller.Update(criado.Id, new ProdutoController.ProdutoBody
                    {
                        Nome = "Novo Nome",
                        TaxaJurosAnual = 33.0m,
                        PrazoMaximoMeses = 23
                    })
                );
                var lido = await db.Produtos.FindAsync(criado.Id);
                Assert.NotNull(lido);
                Assert.Equal("Novo Nome", lido.Nome);
                Assert.Equal(33.0m, lido.TaxaJurosAnual);
                Assert.Equal(23, lido.PrazoMaximoMeses);

            });

        [Fact]
        public async Task ModificarUmProdutoInexistenteDeveRetornarNotFound() =>
            await Util.TesteController<ProdutoController>(async (db, _logger) =>
            {
                var controller = new ProdutoController(db.Object, _logger);
                Assert.IsType<NotFoundResult>(
                    await controller.Update(
                        333,
                        new ProdutoController.ProdutoBody
                        {
                            Nome = "Nome valido",
                            TaxaJurosAnual = 33.0m,
                            PrazoMaximoMeses = 36
                        }));
            });
        [Fact]
        public async Task ModificarProdutoDeveDar400ComParametrosInvalidos() =>
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var controller = new ProdutoController(_db.Object, _logger);
                var result = Assert.IsType<ObjectResult>(
                    await controller.Update(3, new ProdutoController.ProdutoBody
                    {
                        Nome = "2",
                        TaxaJurosAnual = -3,
                        PrazoMaximoMeses = 4
                    })
                );
                Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            });

        [Fact]
        public async Task ModificarProdutoDeveDar500QuandoDaOutraException() =>
            await Util.TesteController<ProdutoController>(async (_db, _logger) =>
            {
                var db = _db.Object;

                db.Produtos.Add(new Produto { });
                await db.SaveChangesAsync();

                _db.Setup(
                    c => c.SaveChangesAsync(It.IsAny<CancellationToken>())
                ).ThrowsAsync(new Exception("Erro simulado"));

                var controller = new ProdutoController(db, _logger);
                var result = Assert.IsType<ObjectResult>(
                    await controller.Update(
                        333,
                        new ProdutoController.ProdutoBody
                        {
                            Nome = "Nome valido",
                            TaxaJurosAnual = 33.0m,
                            PrazoMaximoMeses = 36
                        }));
                Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            });

    }
}