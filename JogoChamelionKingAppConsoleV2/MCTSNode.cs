using System;
using System.Collections.Generic;
using System.Linq;
// A referência à namespace principal é importante para que esta classe possa usar 'Jogo', 'Jogador', etc.
namespace JogoChamelionKingAppConsoleV2
{
    /// <summary>
    /// Representa um único nó na árvore de busca do Monte Carlo (MCTS).
    /// Cada nó armazena um estado específico do jogo e as estatísticas (visitas e vitórias)
    /// das simulações que passaram por ele, permitindo à IA avaliar a qualidade das jogadas.
    /// </summary>
    public class MCTSNode
    {
        #region Propriedades do Nó

        /// <summary>
        /// O estado do jogo (snapshot) que este nó representa. É uma fotografia do jogo
        /// em um determinado ponto da árvore de busca.
        /// </summary>
        public Jogo GameState { get; set; }

        /// <summary>
        /// Referência ao nó pai na árvore. É null para o nó raiz.
        /// Essencial para a fase de Backpropagation.
        /// </summary>
        public MCTSNode Parent { get; set; }

        /// <summary>
        /// Lista de nós filhos, representando os estados de jogo alcançáveis a partir deste nó.
        /// </summary>
        public List<MCTSNode> Children { get; set; }

        /// <summary>
        /// O número de vezes que este nó foi visitado durante a fase de Seleção.
        /// </summary>
        public int Visits { get; set; }

        /// <summary>
        /// O número de vitórias registradas nas simulações que passaram por este nó.
        /// "Vitória" é sempre do ponto de vista da IA que iniciou a busca MCTS.
        /// </summary>
        public double Wins { get; set; }

        /// <summary>
        /// O índice da ação (carta jogada) que foi tomada no nó PAI para chegar a ESTE nó.
        /// -1 significa "nenhuma carta jogada". -2 é o valor padrão para o nó raiz.
        /// </summary>
        public int ActionIndex { get; set; }

        /// <summary>
        /// O índice do jogador (0 ou 1) que realizou a ação para criar este nó.
        /// </summary>
        public int PlayerIndexWhoActed { get; set; }

        #endregion

        #region Campos Privados

        // Instância de Random para escolher ações durante a expansão.
        private Random _nodeRandom = new Random();

        // Lista de ações possíveis (índices de cartas) que ainda não foram exploradas a partir deste nó.
        private List<int> _untriedActions;

        #endregion

        /// <summary>
        /// Construtor para criar um novo nó MCTS.
        /// </summary>
        /// <param name="gameState">O estado do jogo que este nó representa.</param>
        /// <param name="parent">O nó pai (opcional, null para a raiz).</param>
        /// <param name="actionIndex">A ação que levou a este nó (opcional).</param>
        /// <param name="playerIndexWhoActed">O jogador que realizou a ação (opcional).</param>
        public MCTSNode(Jogo gameState, MCTSNode parent = null, int actionIndex = -2, int playerIndexWhoActed = -1)
        {
            GameState = gameState;
            Parent = parent;
            ActionIndex = actionIndex;
            PlayerIndexWhoActed = playerIndexWhoActed;
            Children = new List<MCTSNode>();
            Visits = 0;
            Wins = 0;

            // Se o jogo não acabou e há um jogador definido para jogar,
            // inicializa a lista de ações que ainda podem ser tentadas a partir deste estado.
            if (!GameState.JogoAcabou && GameState.CurrentPlayerIndex != -1)
            {
                // Pega a lista de jogadas possíveis para o jogador da vez neste estado do jogo.
                _untriedActions = GameState.GetPossibleActionsForPlayer(GameState.jogadores[GameState.CurrentPlayerIndex]);
            }
            else
            {
                // Se o jogo acabou ou não há jogador da vez, não há ações a serem tentadas.
                _untriedActions = new List<int>();
            }
        }

        #region Propriedades de Status

        /// <summary>
        /// Verdadeiro se o estado do jogo neste nó é um estado final (alguém venceu).
        /// Nós terminais não podem ser expandidos.
        /// </summary>
        public bool IsTerminalNode => GameState.JogoAcabou;

        /// <summary>
        /// Verdadeiro se todas as ações possíveis a partir deste nó já foram exploradas
        /// (ou seja, todos os nós filhos já foram criados).
        /// </summary>
        public bool IsFullyExpanded => _untriedActions.Count == 0;

        #endregion

        #region Métodos do Algoritmo MCTS

        /// <summary>
        /// Fase de EXPANSÃO: Cria e adiciona um novo nó filho a partir de uma das ações ainda não tentadas.
        /// </summary>
        /// <returns>O novo nó filho criado, ou null se o nó já está totalmente expandido.</returns>
        public MCTSNode Expand()
        {
            // Se não há mais ações para tentar, não é possível expandir.
            if (_untriedActions.Count == 0) return null;

            // Escolhe uma ação aleatória da lista de ações não tentadas.
            int actionToTry = _untriedActions[_nodeRandom.Next(_untriedActions.Count)];
            // Remove a ação da lista para que não seja tentada novamente a partir deste nó.
            _untriedActions.Remove(actionToTry);

            // Clona o estado do jogo para simular a ação sem afetar o estado original do nó pai.
            Jogo nextState = GameState.Clone();
            Jogador actingPlayer = nextState.jogadores[nextState.CurrentPlayerIndex];

            // Simula a ação escolhida (jogar uma carta).
            if (actionToTry != -1) // -1 é a ação de "não jogar carta".
            {
                var cartaUsada = actingPlayer.Mao[actionToTry];
                actingPlayer.TransformarPeca(cartaUsada.Tipo, true);

                // Remove a carta da mão do jogador de forma segura.
                var newMao = new List<Carta>(actingPlayer.Mao);
                newMao.RemoveAt(actionToTry);
                actingPlayer.Mao = newMao;
            }

            // Após a transformação (ou não), o jogador se move.
            actingPlayer.Mover(nextState.casasCoringas, (j, s) => nextState.AplicarEfeitoCoringa(j, s), true);

            // Verifica as consequências do movimento (colisão e vitória).
            nextState.VerificarColisaoEComer(actingPlayer, true);
            if (actingPlayer.Venceu)
            {
                nextState.JogoAcabou = true;
            }

            // Se o jogo não terminou, reseta a peça do jogador para o próximo turno da simulação.
            // O CurrentPlayerIndex não é alterado aqui, pois a simulação aleatória (playout) cuidará
            // da lógica de turnos baseada nos dados.
            if (!nextState.JogoAcabou)
            {
                actingPlayer.ResetarPeca();
            }

            // Cria o novo nó filho com o estado resultante da ação.
            MCTSNode child = new MCTSNode(nextState, this, actionToTry, nextState.CurrentPlayerIndex);
            Children.Add(child);
            return child;
        }

        /// <summary>
        /// Fase de SELEÇÃO: Escolhe o melhor nó filho para explorar, usando a fórmula UCB1.
        /// A fórmula equilibra exploração (tentar jogadas menos visitadas) e explotação (focar em jogadas com alta taxa de vitória).
        /// </summary>
        /// <param name="explorationParam">O parâmetro de exploração (C). O valor padrão (sqrt(2)) é comum.</param>
        /// <returns>O nó filho mais promissor.</returns>
        public MCTSNode SelectChildUCB(double explorationParam = 1.414)
        {
            if (Children.Count == 0) return null;

            return Children.OrderByDescending(c =>
            {
                // Se um filho nunca foi visitado, ele tem prioridade máxima para ser explorado.
                // Retornar infinito garante que ele seja escolhido.
                if (c.Visits == 0) return double.PositiveInfinity;

                // --- Fórmula UCB1 (Upper Confidence Bound 1) ---
                // Parte de Explotação: A taxa de vitórias observada. (c.Wins / c.Visits)
                // Quanto maior a taxa de vitórias, mais atraente é o nó.
                double winRate = c.Wins / c.Visits;

                // Parte de Exploração: Dá um bônus a nós menos visitados.
                // Math.Log(this.Visits) = logaritmo das visitas do nó PAI.
                // c.Visits = visitas do nó FILHO.
                // O bônus diminui à medida que o filho é mais visitado.
                double explorationBonus = explorationParam * Math.Sqrt(Math.Log(this.Visits) / c.Visits);

                // O valor UCB é a soma da explotação com o bônus de exploração.
                return winRate + explorationBonus;
            }).First();
        }

        /// <summary>
        /// Fase de RETROPROPAGAÇÃO: Atualiza as estatísticas (visitas e vitórias)
        /// deste nó e de todos os seus ancestrais até a raiz.
        /// </summary>
        /// <param name="result">O resultado da simulação (1.0 para vitória da IA, 0.0 para derrota).</param>
        public void Backpropagate(double result)
        {
            MCTSNode node = this;
            // Percorre a árvore de baixo para cima, do nó atual até a raiz.
            while (node != null)
            {
                // Incrementa o número de visitas para cada nó no caminho.
                node.Visits++;

                // Adiciona o resultado da simulação às vitórias. Como 'result' é sempre
                // da perspectiva da IA principal, simplesmente somamos o valor.
                node.Wins += result;

                // Move para o nó pai para continuar a atualização.
                node = node.Parent;
            }
        }

        #endregion
    }
}