namespace JogoChamelionKingAppConsoleV2
{
    public class Carta
    {
        public TipoPeca Tipo { get; set; }
        public string Descricao { get; set; }
        public int QuantidadeMovimento { get; set; }

        public Carta(TipoPeca tipo, string descricao, int quantidadeMovimento)
        {
            Tipo = tipo;
            Descricao = descricao;
            QuantidadeMovimento = quantidadeMovimento;
        }

        public Carta Clone()
        {
            return new Carta(Tipo, Descricao, QuantidadeMovimento);
        }
    }
}