using System.Text.Json.Serialization;
using Simulador.Core.Exceptions;

namespace Simulador.Core.Models
{
    public class Pagamento
    {
        [JsonPropertyName("mes")]
        public int Mes { get; set; }
        [JsonPropertyName("saldoDevedorInicial")]
        public decimal SaldoDevedorInicial { get; set; }
        [JsonPropertyName("saldoDevedorFinal")]
        public decimal SaldoDevedorFinal { get; set; }
        [JsonPropertyName("juros")]
        public decimal Juros { get; set; }
        [JsonPropertyName("amortizacao")]
        public decimal Amortizacao { get; set; }
    }
    public class Simulacao
    {
        public const string E_VALOR_SOLICITADO_INVALIDO = "O valor deve ser pelo menos 1.00";
        public const string E_PRAZO_CURTO_DEMAIS = "O prazo é inferior ao mínimo";
        public const string E_PRAZO_LONGO_DEMAIS = "O prazo é mais longo do que o máximo do produto";

        [JsonPropertyName("produto")]
        public required Produto Produto { get; set; }

        [JsonPropertyName("valorSolicitado")]
        public decimal ValorSolicitado { get; set; }
        
        [JsonPropertyName("prazoMeses")]
        public int PrazoMeses { get; set; }

        [JsonPropertyName("taxaJurosEfetivaMensal")]
        public decimal TaxaJurosEfetivaMensal { get; set; }
        [JsonPropertyName("valorTotalComJuros")]
        public decimal ValorTotalComJuros { get; set; }
        [JsonPropertyName("parcelaMensal")]
        public decimal ParcelaMensal { get; set; }
        [JsonPropertyName("memoriaCalculo")]
        public List<Pagamento> MemoriaCalculo { get; } = [];

        public void Validar()
        {
            Produto.Validar();
            if (ValorSolicitado < 1.0m)
            {
                throw new SimuladorException(E_VALOR_SOLICITADO_INVALIDO);
            }
            if (PrazoMeses < Produto.PRAZO_MINIMO)
            {
                throw new SimuladorException(E_PRAZO_CURTO_DEMAIS);
            }
            if (PrazoMeses > Produto.PrazoMaximoMeses)
            {
                throw new SimuladorException(E_PRAZO_LONGO_DEMAIS);
            }
        }

        public void CalcularPagamentos()
        {
            MemoriaCalculo.Clear();
            Validar();

            var jurosMensais = Math.Pow(1 + (double)Produto.TaxaJurosAnual / 100.0, 1.0 / 12.0) - 1.0;

            var fator = jurosMensais / (1 - Math.Pow(1 + jurosMensais, -PrazoMeses));

            var parcelaMensal = (decimal)Math.Max(0.01,Math.Round((double)ValorSolicitado * fator, 2, MidpointRounding.AwayFromZero));

            TaxaJurosEfetivaMensal = (decimal)Math.Round(jurosMensais, 6, MidpointRounding.AwayFromZero);
            ParcelaMensal = parcelaMensal;
            ValorTotalComJuros = ParcelaMensal * PrazoMeses;
            var saldoDevedor = ValorSolicitado;
            for (int i = 0; i < PrazoMeses; i++)
            {
                var saldoDevedorInicial = saldoDevedor;
                var juros = Math.Round(saldoDevedor * (decimal)jurosMensais,2,MidpointRounding.AwayFromZero);
                var amortizacao = parcelaMensal - juros;
                saldoDevedor -= amortizacao;
                var saldoDevedorFinal = saldoDevedor;
                /*
                    pequeno ajuste contábil
                    como não tem precisão infinita, na última parcela
                    fatalmente a pessoa vai terminar devendo alguns centavos
                    ou tendo pago alguns centavos a mais
                    então faço isso só para o que a pessoa pagou bater
                    com parcela * prazo 
                */
                if (i == PrazoMeses - 1)
                {
                    amortizacao += saldoDevedorFinal;
                    juros -= saldoDevedorFinal;
                    saldoDevedorFinal = 0;
                }
                MemoriaCalculo.Add(new Pagamento
                {
                    Mes = i + 1,
                    Juros = juros,
                    Amortizacao = amortizacao,
                    SaldoDevedorInicial = saldoDevedorInicial,
                    SaldoDevedorFinal = saldoDevedorFinal
                });
            }
        }
    }
}