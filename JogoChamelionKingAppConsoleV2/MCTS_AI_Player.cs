using System;
using System.Collections.Generic; // Necessário para List
using System.Linq; // Necessário para .OrderByDescending, .Any, etc.

namespace JogoChamelionKingAppConsoleV2
{
    /// <summary>
    /// Representa um jogador controlado por Inteligência Artificial que utiliza o algoritmo
    /// MCTS (Monte Carlo Tree Search) para decidir a melhor jogada.
    /// O MCTS é um algoritmo de busca heurística que constrói uma árvore de busca de jogos
    /// para encontrar a jogada mais promissora.
    /// </summary>
    public class MCTS_AI_Player
    {
        // Instância de Random privada para uso exclusivo da IA nas simulações.
        private Random _aiRandom = new Random();

        // O índice (0 ou 1) que esta instância de IA representa na lista de jogadores do jogo.
        private int _mctsAiPlayerIndex;

        /// <summary>
        /// Construtor da IA.
        /// </summary>
        /// <param name="aiPlayerIndex">O índice (0 ou 1) que este jogador de IA ocupará.</param>
        public MCTS_AI_Player(int aiPlayerIndex)
        {
            _mctsAiPlayerIndex = aiPlayerIndex;
        }

        /// <summary>
        /// O método principal que executa o algoritmo MCTS para determinar a melhor ação (índice da carta a ser jogada).
        /// </summary>
        /// <param name="currentActualGameState">O estado atual e real do jogo.</param>
        /// <param name="iterations">O número de iterações que o MCTS executará. Mais iterações = melhor decisão, mas mais tempo de processamento.</param>
        /// <returns>O índice da melhor carta para jogar (-1 para não jogar nenhuma).</returns>
        public int GetBestAction(Jogo currentActualGameState, int iterations)
        {
            // Clona o estado atual do jogo para não modificar o jogo real. O MCTS trabalha sobre esta cópia.
            Jogo rootStateClone = currentActualGameState.Clone();

            // Define que o jogador atual no estado clonado é a IA, pois estamos decidindo a ação para ela.
            rootStateClone.CurrentPlayerIndex = _mctsAiPlayerIndex;

            // Cria o nó raiz da árvore de busca, representando o estado atual do jogo.
            MCTSNode root = new MCTSNode(rootStateClone, playerIndexWhoActed: -1); // -1 indica que ninguém agiu para chegar a este estado (é o início).

            // Loop principal do MCTS. Cada iteração melhora a avaliação das jogadas.
            for (int i = 0; i < iterations; i++)
            {
                MCTSNode node = root;

                // --- 1. SELEÇÃO (Selection) ---
                // Navega pela árvore a partir da raiz, escolhendo os filhos mais promissores (com maior valor UCB)
                // até encontrar um nó que não esteja totalmente expandido ou que seja terminal.
                while (!node.IsTerminalNode && node.IsFullyExpanded)
                {
                    node = node.SelectChildUCB();
                    // Fallback para o caso de não haver filhos para selecionar (raro).
                    if (node == null)
                    {
                        node = root;
                        break;
                    }
                }

                // --- 2. EXPANSÃO (Expansion) ---
                // Se o nó selecionado não for um estado final e tiver ações não tentadas,
                // um novo nó filho é criado a partir de uma dessas ações.
                if (!node.IsTerminalNode && !node.IsFullyExpanded)
                {
                    MCTSNode expandedChild = node.Expand();
                    if (expandedChild != null)
                    {
                        // O novo nó expandido se torna o ponto de partida para a simulação.
                        node = expandedChild;
                    }
                }

                // --- 3. SIMULAÇÃO (Simulation) ---
                // A partir do nó selecionado (ou recém-expandido), um jogo completo é simulado
                // com jogadas aleatórias (playout) até que um resultado (vitória/derrota) seja alcançado.
                double simulationResult = SimulateRandomPlayout(node.GameState.Clone());

                // --- 4. RETROPROPAGAÇÃO (Backpropagation) ---
                // O resultado da simulação (1.0 para vitória, 0.0 para derrota) é propagado de volta
                // pela árvore, do nó simulado até a raiz, atualizando as estatísticas (visitas e vitórias) de cada nó no caminho.
                node.Backpropagate(simulationResult);
            }

            // Após todas as iterações, se não houver filhos, significa que nenhuma ação foi explorada (ex: sem cartas na mão).
            if (root.Children.Count == 0)
            {
                Console.WriteLine($"[MCTS AI {currentActualGameState.jogadores[_mctsAiPlayerIndex].Nome}]: Sem filhos explorados, nenhuma carta será jogada.");
                return -1; // Retorna a ação de "não jogar".
            }

            // Seleciona o melhor filho do nó raiz. A melhor ação é aquela que levou ao nó filho mais visitado.
            // O número de visitas é um indicador mais robusto da qualidade de uma jogada do que a taxa de vitórias.
            // Em caso de empate, a maior taxa de vitórias é usada como critério de desempate.
            MCTSNode bestChild = root.Children.OrderByDescending(c => c.Visits).ThenByDescending(c => c.Wins).FirstOrDefault();

            if (bestChild == null)
            {
                Console.WriteLine($"[MCTS AI {currentActualGameState.jogadores[_mctsAiPlayerIndex].Nome}]: Não foi possível selecionar o melhor filho, nenhuma carta será jogada.");
                return -1; // Fallback
            }

            // Retorna o índice da ação (carta) que corresponde ao melhor nó filho encontrado.
            return bestChild.ActionIndex;
        }

        /// <summary>
        /// Simula um jogo com jogadas aleatórias (playout) a partir de um estado de jogo fornecido.
        /// </summary>
        /// <param name="gameStateToSimulate">O estado do jogo a partir do qual a simulação começará.</param>
        /// <returns>1.0 se a IA venceu a simulação, 0.0 caso contrário.</returns>
        private double SimulateRandomPlayout(Jogo gameStateToSimulate)
        {
            // Clona o estado para garantir que a simulação não afete outros galhos da árvore MCTS.
            Jogo simGame = gameStateToSimulate.Clone();

            int safetyBreak = 0; // Um contador de segurança para evitar loops infinitos.
            const int maxSimulationRounds = 50; // Limite máximo de rodadas para a simulação.

            // A simulação continua até o jogo acabar ou o limite de rodadas ser atingido.
            while (!simGame.JogoAcabou && safetyBreak < maxSimulationRounds)
            {
                // Simula uma rodada completa: rolagem de dados para ver quem joga.
                int simDado1 = _aiRandom.Next(1, 7);
                int simDado2 = _aiRandom.Next(1, 7);
                Jogador roundWinnerSim = null;
                int roundWinnerIndexSim = -1;

                if (simDado1 > simDado2) { roundWinnerSim = simGame.jogadores[0]; roundWinnerIndexSim = 0; }
                else if (simDado2 > simDado1) { roundWinnerSim = simGame.jogadores[1]; roundWinnerIndexSim = 1; }

                simGame.CurrentPlayerIndex = roundWinnerIndexSim;

                // Se houver um vencedor da rodada (não foi empate nos dados).
                if (roundWinnerSim != null)
                {
                    // Verifica se o jogador está bloqueado.
                    if (roundWinnerSim.RodadasBloqueado > 0)
                    {
                        roundWinnerSim.RodadasBloqueado--; // Apenas gasta o turno de bloqueio.
                    }
                    else
                    {
                        // Lógica de uma jogada normal na simulação.
                        simGame.ComprarCarta(roundWinnerSim, true); // Compra uma carta (em modo silencioso).

                        // Decide aleatoriamente se vai usar uma carta (50% de chance).
                        int cardChoiceSim = -1;
                        if (roundWinnerSim.Mao.Any() && _aiRandom.Next(0, 2) == 0)
                        {
                            cardChoiceSim = _aiRandom.Next(0, roundWinnerSim.Mao.Count); // Escolhe uma carta aleatória da mão.
                        }

                        // Se uma carta foi escolhida, ela é usada.
                        if (cardChoiceSim != -1)
                        {
                            var cartaUsadaSim = roundWinnerSim.Mao[cardChoiceSim];
                            roundWinnerSim.TransformarPeca(cartaUsadaSim.Tipo, true);

                            // Remove a carta da mão de forma segura.
                            var newMaoSim = new List<Carta>(roundWinnerSim.Mao);
                            newMaoSim.RemoveAt(cardChoiceSim);
                            roundWinnerSim.Mao = newMaoSim;
                        }

                        // O jogador se move e as regras de colisão/coringa são aplicadas.
                        roundWinnerSim.Mover(simGame.casasCoringas, (j, s) => simGame.AplicarEfeitoCoringa(j, s), true);
                        simGame.VerificarColisaoEComer(roundWinnerSim, true);

                        // Verifica se o movimento resultou em vitória.
                        if (roundWinnerSim.Venceu) simGame.JogoAcabou = true;
                    }
                }

                // Se o jogo não acabou na rodada, as peças voltam ao estado padrão (Rei).
                if (!simGame.JogoAcabou)
                {
                    foreach (var j in simGame.jogadores) j.ResetarPeca();
                }
                safetyBreak++;
            }

            // Ao final da simulação, verifica se o jogador da IA foi o vencedor.
            if (simGame.JogoAcabou && simGame.jogadores[_mctsAiPlayerIndex].Venceu)
            {
                return 1.0; // Retorna 1.0 para uma vitória.
            }
            return 0.0; // Retorna 0.0 para derrota ou empate/jogo não concluído.
        }
    }
}