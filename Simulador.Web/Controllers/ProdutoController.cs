using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Simulador.Core.Data;
using Simulador.Core.Exceptions;
using Simulador.Core.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Simulador.Web.Controllers
{

    [ApiController]
    [Route("/produtos")]
    [SwaggerTag("Serve para criar/ver/editar/remover os produtos de empréstimos")]
    [Produces("application/json")]
    public class ProdutoController(SimuladorDbContext ctx, ILogger<ProdutoController> logger) : ControllerBase
    {
        public const string E_ERRO_INTERNO = "alguma coisa deu muito errado. checar os logs.";

        public record ProdutoBody
        {
            [JsonPropertyName("nome")]
            public string Nome { get; set; } = "";

            [JsonPropertyName("prazoMaximoMeses")]
            public int PrazoMaximoMeses { get; set; }

            [JsonPropertyName("taxaJurosAnual")]
            public decimal TaxaJurosAnual { get; set; }

            public Produto ToProduto() => new()
            {
                Nome = Nome,
                PrazoMaximoMeses = PrazoMaximoMeses,
                TaxaJurosAnual = TaxaJurosAnual
            };
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Ver uma lista de todos os produtos")]
        [SwaggerResponse(StatusCodes.Status200OK, "Lista de produtos", typeof(Produto[]))]
        public async Task<IActionResult> Index() => Ok(await ctx.Produtos.ToListAsync());

        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Ver os detalhes de um produto")]
        [SwaggerResponse(StatusCodes.Status200OK, "Se o produto existe", typeof(Produto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Se o produto não existe")]
        public async Task<IActionResult> GetOne(int id)
        {
            if (await ctx.Produtos.FindAsync(id) is Produto p)
            {
                return Ok(p);
            }
            return NotFound();
        }

        [HttpPost]
        [SwaggerOperation(Summary = "Criar um produto")]
        [SwaggerResponse(StatusCodes.Status201Created, "Se o produto existe", typeof(Produto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Se os parâmetros são inválidos")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Se não deu para criar o produto por outro motivo")]
        public async Task<IActionResult> Create(ProdutoBody creationBody)
        {
            var tmp_produto = creationBody.ToProduto();
            try
            {
                tmp_produto.Validar();
                ctx.Produtos.Add(tmp_produto);
                await ctx.SaveChangesAsync();
                return CreatedAtAction(nameof(GetOne), new { id = tmp_produto.Id }, tmp_produto);
            }
            catch (SimuladorException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "erro criando produto");
                return Problem(E_ERRO_INTERNO, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(Summary = "Deletar um produto")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Se o produto existia e foi deletado")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Se o produto não existe")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Se não deu para apagar o produto do banco de dados")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                if (await ctx.Produtos.FindAsync(id) is Produto p)
                {
                    ctx.Produtos.Remove(p);
                    await ctx.SaveChangesAsync();
                    return NoContent();
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "erro deletando produto");
                return Problem(E_ERRO_INTERNO, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Modificar um produto")]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Se a modificação deu certo")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Se os parâmetros são inválidos")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Se o produto não existe")]
        [SwaggerResponse(StatusCodes.Status500InternalServerError, "Se deu algum outro erro inesperado")]
        public async Task<IActionResult> Update(int id, ProdutoBody produto)
        {
            var tmp_produto = produto.ToProduto();
            tmp_produto.Id = id;
            try
            {
                tmp_produto.Validar();
                ctx.Update(tmp_produto);
                await ctx.SaveChangesAsync();
            }
            catch (SimuladorException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            /* update em uma coisa com id que não existe normalmente cai aqui */
            catch (DbUpdateException)
            {
                return NotFound();
            }
            /* mas pode cair aqui também, só que o motivo é alguma coisa relacionada
             a modificações concorrentes */
            catch (DBConcurrencyException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, "erro modificando produto");
                return Problem(E_ERRO_INTERNO, statusCode: StatusCodes.Status500InternalServerError);
            }
            return NoContent();
        }
    }
}