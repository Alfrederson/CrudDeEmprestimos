using System.Text.Json;
using Simulador.Core.Exceptions;
using Simulador.Core.Models;
using Simulador.Web;
using Xunit.Abstractions;

namespace Simulador.Tests;

public class CoreTests(ITestOutputHelper helper)
{
    private readonly ITestOutputHelper _output = helper;

    [Fact]
    public void TestarConstantesDoProduto()
    {
        Assert.True(Produto.PRAZO_MINIMO > 0);
        Assert.True(Produto.PRAZO_MAXIMO > 0);
        Assert.True(Produto.PRAZO_MAXIMO > Produto.PRAZO_MINIMO);
        Assert.True(Produto.TAXA_JUROS_MAXIMA > Produto.TAXA_JUROS_MINIMA);
    }

    [Theory]
    [InlineData("x", 12, 18.0)]
    [InlineData("xxxxxxxxxxyyyyyyyyyyxxxxxxxxxxyyyyyyyyyyxxxxxxxxxxyyyyyyyyyy", 12, 18.0)]
    [InlineData("Nome Valido", -1, 0.0)]
    [InlineData("Nome Valido", -1, -18.0)]
    [InlineData("Nome Valido", 10, -18.0)]   
    [InlineData("Nome Valido", 10, 900.0)]   
    public void ProdutoDeveValidarParametros(string nome, int prazo, decimal juros)
    {
        Assert.Throws<SimuladorException>(() =>
        {
            new Produto { Nome = nome, TaxaJurosAnual = juros, PrazoMaximoMeses = prazo }.Validar();
        });
    }


    [Theory]
    [InlineData(-1, 1000.0)]
    [InlineData(14, 1000.0)]
    [InlineData(7, -100.0)]
    [InlineData(7, 0.9)]

    public void SimulacaoDeveValidarParametros(int prazo, decimal valorSolicitado)
    {
        var produto = new Produto { Nome = "Emprestimo Valido", TaxaJurosAnual = 18, PrazoMaximoMeses = 12 };
        Assert.Throws<SimuladorException>(() =>
        {
            new Simulacao { Produto = produto, ValorSolicitado = valorSolicitado, PrazoMeses = prazo }.Validar();
        });
    }


    [Theory]
    [InlineData(12, 10000.0)]
    public void SimulacaoDeveProduzirValoresConsistentes(int prazo, decimal valorSolicitado)
    {
        var produto = new Produto { Nome = "Emprestimo Camarada", TaxaJurosAnual = 18, PrazoMaximoMeses = 36 };
        var simulacao = new Simulacao { Produto = produto, ValorSolicitado = valorSolicitado, PrazoMeses = prazo };
        simulacao.CalcularPagamentos();

        Assert.True(
            simulacao.ValorTotalComJuros > simulacao.ValorSolicitado,
            String.Format(
                "O valor total ({0}) deveria ser maior do que o solicitado ({1})",
                simulacao.ValorTotalComJuros,
                simulacao.ValorSolicitado
            )
        );

        decimal soma_total = 0.0m;
        simulacao.MemoriaCalculo.ForEach(pagamento =>
        {
            soma_total += pagamento.Amortizacao + pagamento.Juros;
        });

        Assert.Equal(soma_total, simulacao.ValorTotalComJuros);
    }

    [Theory]
    [InlineData(12, 1192.12)]
    [InlineData(11, 3210.0)]
    [InlineData(6, 1430.0)]
    [InlineData(31, 1120.0)]

    public void UltimaParcelaDeveQuitarEmprestimo(int prazo, decimal valor)
    {
        var produto = new Produto { Nome = "Emprestimo Camarada", TaxaJurosAnual = 18, PrazoMaximoMeses = 36 };
        var simulacao = new Simulacao { Produto = produto, ValorSolicitado = valor, PrazoMeses = prazo };
        simulacao.CalcularPagamentos();

        Assert.Equal(0.0m, simulacao.MemoriaCalculo[prazo - 1].SaldoDevedorFinal);
    }


    [Theory]
    [InlineData(12, 1192.12)]
    [InlineData(11, 3210.0)]
    [InlineData(6, 1430.0)]
    [InlineData(31, 1120.0)]
    public void SimulacaoDeveTerNumeroCorretoDeParcelas(int prazo, decimal valor)
    {
        var produto = new Produto { Nome = "Emprestimo funci", TaxaJurosAnual = 18, PrazoMaximoMeses = 36 };
        var simulacao = new Simulacao { Produto = produto, ValorSolicitado = valor, PrazoMeses = prazo };
        simulacao.CalcularPagamentos();
        Assert.Equal(simulacao.MemoriaCalculo.Count, prazo);
        _output.WriteLine(Util.EncodeJson(simulacao));
    }
}
