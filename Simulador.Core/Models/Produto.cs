using System.Text.Json.Serialization;
using Simulador.Core.Exceptions;

namespace Simulador.Core.Models
{
    public class Produto
    {
        public const int NOME_COMPRIMENTO_MINIMO = 5;
        public const int NOME_COMPRIMENTO_MAXIMO = 50;
        public const decimal TAXA_JUROS_MINIMA = 0.0m;
        public const decimal TAXA_JUROS_MAXIMA = 100.0m;
        public const int PRAZO_MINIMO = 1;
        public const int PRAZO_MAXIMO = 720;

        public const string E_NOME_COMPRIMENTO_INVALIDO = "O nome do produto deve ter entre 5 e 50 caracteres";
        public const string E_TAXA_JUROS_INVALIDA = "A taxa de juros anual deve estar entre 0 e 100%";
        public const string E_PRAZO_INVALIDO = "O prazo máximo deve estar entre 1 e 720 meses";

        [JsonPropertyName("id")]
        public int Id { get; set; }
        private string _nome = "";

        [JsonPropertyName("nome")]
        public string Nome
        {
            get => _nome;
            set => _nome = value.Trim();
        }

        [JsonPropertyName("taxaJurosAnual")]
        public decimal TaxaJurosAnual { get; set; }

        [JsonPropertyName("prazoMaximoMeses")]
        public int PrazoMaximoMeses { get; set; }

        public void Validar()
        {
            if (Nome.Length < NOME_COMPRIMENTO_MINIMO || Nome.Length > NOME_COMPRIMENTO_MAXIMO)
            {
                throw new SimuladorException(E_NOME_COMPRIMENTO_INVALIDO);
            }
            if (TaxaJurosAnual < TAXA_JUROS_MINIMA || TaxaJurosAnual > TAXA_JUROS_MAXIMA)
            {
                throw new SimuladorException(E_TAXA_JUROS_INVALIDA);
            }
            if (PrazoMaximoMeses < PRAZO_MINIMO || PrazoMaximoMeses > PRAZO_MAXIMO)
            {
                throw new SimuladorException(E_PRAZO_INVALIDO);
            }
        }
    }
}