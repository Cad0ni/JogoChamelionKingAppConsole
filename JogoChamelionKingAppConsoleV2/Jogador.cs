using System;
using System.Collections.Generic;
using System.Linq;

namespace JogoChamelionKingAppConsoleV2
{
    /// <summary>
    /// Representa um jogador no jogo. Esta classe armazena todas as informações e estados
    /// pertinentes a um jogador, como seu nome, posição no tabuleiro, mão de cartas e a peça
    /// em que está transformado. Também contém os métodos para as ações que um jogador pode realizar.
    /// </summary>
    public class Jogador
    {
        #region Propriedades do Jogador

        /// <summary>
        /// O nome do jogador para identificação.
        /// </summary>
        public string Nome { get; set; }

        /// <summary>
        /// A lista de objetos 'Carta' que o jogador possui em sua mão.
        /// </summary>
        public List<Carta> Mao { get; set; }

        /// <summary>
        /// A peça de xadrez atual em que o jogador está transformado. Determina quantos espaços ele se move.
        /// </summary>
        public TipoPeca PecaAtual { get; set; }

        /// <summary>
        /// A posição atual do jogador no tabuleiro (de 1 a 64).
        /// </summary>
        public int Posicao { get; set; }

        /// <summary>
        /// Uma flag que se torna verdadeira quando o jogador alcança ou ultrapassa a posição 64.
        /// </summary>
        public bool Venceu { get; set; }

        /// <summary>
        /// Um contador para o número de rodadas que o jogador ficará bloqueado, sem poder agir.
        /// </summary>
        public int RodadasBloqueado { get; set; }

        /// <summary>
        /// Uma flag que indica se este jogador é controlado pela Inteligência Artificial.
        /// </summary>
        public bool IsAI { get; set; }

        #endregion

        /// <summary>
        /// Construtor da classe Jogador. Inicializa um novo jogador com os valores padrão de início de jogo.
        /// </summary>
        /// <param name="nome">O nome a ser atribuído ao jogador.</param>
        /// <param name="isAI">Define se o jogador será controlado pela IA.</param>
        public Jogador(string nome, bool isAI = false)
        {
            Nome = nome;
            Mao = new List<Carta>();
            PecaAtual = TipoPeca.ReiCamaleao; // Todos os jogadores começam como a peça padrão.
            Posicao = 1; // Posição inicial.
            Venceu = false;
            RodadasBloqueado = 0;
            IsAI = isAI;
        }

        #region Ações do Jogador

        /// <summary>
        /// Adiciona uma carta à mão do jogador.
        /// </summary>
        /// <param name="carta">A carta a ser recebida.</param>
        /// <param name="silent">Se verdadeiro, não imprime mensagens no console. Útil para a IA.</param>
        public void ReceberCarta(Carta carta, bool silent = false)
        {
            Mao.Add(carta);
            if (!silent) Console.WriteLine($"{Nome} recebeu uma carta: {carta.Descricao}");
        }

        /// <summary>
        /// Altera a peça atual do jogador, transformando-o.
        /// </summary>
        /// <param name="novaPeca">O novo tipo de peça para se transformar.</param>
        /// <param name="silent">Se verdadeiro, a transformação ocorre sem feedback no console.</param>
        public void TransformarPeca(TipoPeca novaPeca, bool silent = false)
        {
            PecaAtual = novaPeca;
            if (!silent) Console.WriteLine($"{Nome} se transformou em {novaPeca}");
        }

        /// <summary>
        /// Reseta a peça do jogador para o estado padrão (ReiCamaleao) no final de cada rodada.
        /// </summary>
        public void ResetarPeca()
        {
            PecaAtual = TipoPeca.ReiCamaleao;
        }

        /// <summary>
        /// Executa o movimento do jogador no tabuleiro baseado em sua peça atual.
        /// Também verifica se o jogador caiu em uma casa coringa.
        /// </summary>
        /// <param name="casasCoringas">A lista de posições que são casas coringas.</param>
        /// <param name="aplicarEfeito">Uma função (delegate) que executa a lógica do efeito coringa, vinda da classe Jogo.</param>
        /// <param name="silent">Se verdadeiro, o movimento e seus efeitos ocorrem sem feedback no console.</param>
        public void Mover(List<int> casasCoringas, Action<Jogador, bool> aplicarEfeito, bool silent = false)
        {
            int casas = ObterMovimento();
            if (casas > 0)
            {
                Posicao += casas;
                if (!silent) Console.WriteLine($"{Nome} moveu {casas} casas. Nova posição: {Posicao}");

                // Loop para lidar com o caso de um efeito coringa levar a outra casa coringa.
                while (casasCoringas.Contains(Posicao))
                {
                    // Aplica o efeito coringa passando este próprio objeto 'Jogador' como argumento.
                    aplicarEfeito(this, silent);
                    // Se o efeito coringa resultou em uma vitória, interrompe o movimento imediatamente.
                    if (Venceu) return;
                }

                // Após todos os movimentos e efeitos, verifica a condição de vitória.
                if (Posicao >= 64)
                {
                    Venceu = true;
                    // A mensagem de vitória é separada para ter mais destaque.
                    if (!silent) Console.WriteLine($"\n⭐ {Nome} alcançou ou ultrapassou a posição 64 e venceu o jogo! ⭐");
                }
            }
        }

        /// <summary>
        /// Retorna o número de casas que o jogador deve se mover com base em sua peça atual.
        /// Utiliza uma 'switch expression' para um código mais limpo e conciso.
        /// </summary>
        /// <returns>O número de casas para mover.</returns>
        public int ObterMovimento()
        {
            return PecaAtual switch
            {
                TipoPeca.Peao => 1,
                TipoPeca.Cavalo => 4,
                TipoPeca.Torre => 6,
                TipoPeca.Bispo => 8,
                TipoPeca.Dama => 12,
                _ => 0, // O ReiCamaleao (ou qualquer outro caso) não se move por padrão.
            };
        }

        /// <summary>
        /// Exibe o status atual do jogador de forma formatada no console.
        /// </summary>
        public void MostrarStatus()
        {
            string bloqueio = RodadasBloqueado > 0 ? $"(Bloqueado por {RodadasBloqueado} rodada(s))" : "";
            string aiTag = IsAI ? "[AI]" : "";
            Console.WriteLine($"{Nome} {aiTag}: Posição {Posicao} | Peça: {PecaAtual} | Cartas: {Mao.Count} {bloqueio}");
        }

        /// <summary>
        /// Cria um "clone" profundo deste objeto Jogador.
        /// É essencial para a IA (MCTS), que precisa criar cópias independentes do estado do jogo para simulações.
        /// </summary>
        /// <returns>Um novo objeto Jogador que é uma cópia exata do atual.</returns>
        public Jogador Clone()
        {
            // Cria uma nova instância de Jogador com o mesmo nome e flag de IA.
            var clone = new Jogador(Nome, IsAI)
            {
                // Copia as propriedades de tipo de valor diretamente.
                PecaAtual = PecaAtual,
                Posicao = Posicao,
                Venceu = Venceu,
                RodadasBloqueado = RodadasBloqueado
            };

            // Para a lista de cartas (tipo de referência), é necessário criar uma nova lista
            // e clonar cada carta individualmente. Isso garante que a mão do clone seja
            // independente da mão do jogador original (deep copy).
            foreach (var carta in Mao)
            {
                clone.Mao.Add(carta.Clone());
            }
            return clone;
        }

        #endregion
    }
}