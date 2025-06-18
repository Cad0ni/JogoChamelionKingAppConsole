using System;
using System.Collections.Generic;
using System.Linq;

// O namespace encapsula todas as classes relacionadas ao jogo, ajudando a organizar o código.
namespace JogoChamelionKingAppConsoleV2
{
    /// <summary>
    /// A classe 'Jogo' é o motor central do jogo. Ela gerencia o estado geral da partida,
    /// incluindo os jogadores, o baralho de cartas, o tabuleiro (através das posições dos jogadores e casas coringas),
    /// e as regras principais como colisões, efeitos especiais e condições de vitória.
    /// </summary>
    public class Jogo
    {
        // Uma lista para armazenar todos os jogadores que participam da partida.
        public List<Jogador> jogadores;

        // Uma Fila (Queue) para representar o baralho. A Fila é ideal porque simula um baralho real:
        // a primeira carta a entrar é a primeira a sair (FIFO - First-In, First-Out),
        // representando a compra da carta do topo.
        public Queue<Carta> baralho;

        // Instância única do gerador de números aleatórios. É uma boa prática usar uma única instância
        // para garantir uma melhor distribuição de aleatoriedade ao longo do ciclo de vida do programa.
        private Random randomInstance;

        // Propriedade booleana que sinaliza se o jogo terminou. O loop principal do jogo verifica este valor.
        public bool JogoAcabou { get; set; }

        // Lista de inteiros que armazena as posições (números das casas) que terão efeitos coringas.
        public List<int> casasCoringas;

        // Armazena o índice do jogador atual na lista 'jogadores'. Essencial para controlar o fluxo de turnos.
        public int CurrentPlayerIndex { get; set; }

        /// <summary>
        /// Construtor da classe Jogo.
        /// </summary>
        /// <param name="initializeDeckAndWildcards">
        /// Um parâmetro opcional que, quando verdadeiro, inicializa o baralho e as casas coringas.
        /// Útil para criar 'clones' do jogo para simulações de IA sem recriar tudo desnecessariamente.
        /// </param>
        public Jogo(bool initializeDeckAndWildcards = true)
        {
            jogadores = new List<Jogador>();
            baralho = new Queue<Carta>();
            randomInstance = new Random();
            JogoAcabou = false;

            // Se o parâmetro for verdadeiro, o jogo é configurado com um novo baralho e novas casas coringas.
            if (initializeDeckAndWildcards)
            {
                InicializarBaralho();
                InicializarCasasCoringas();
            }

            // O índice do jogador atual começa como -1 para indicar que o jogo ainda não começou.
            CurrentPlayerIndex = -1;
        }

        /// <summary>
        /// Método de acesso público para a instância Random, permitindo que outras partes do código
        /// (como a IA) possam utilizá-la para tomar decisões aleatórias.
        /// </summary>
        public Random GetRandomInstance() => randomInstance;

        /// <summary>
        /// Define as posições das 8 casas coringas no tabuleiro de forma aleatória.
        /// </summary>
        private void InicializarCasasCoringas()
        {
            casasCoringas = new List<int>();
            // Loop para adicionar 8 casas coringas.
            for (int i = 0; i < 8; i++)
            {
                int posicao;
                // Garante que a posição gerada não seja repetida.
                do { posicao = randomInstance.Next(1, 65); } while (casasCoringas.Contains(posicao));
                casasCoringas.Add(posicao);
            }
        }

        /// <summary>
        /// Cria e embaralha o baralho de cartas do jogo.
        /// </summary>
        private void InicializarBaralho()
        {
            var todasCartas = new List<Carta>();
            // Adiciona a quantidade específica de cada tipo de carta ao baralho.
            for (int i = 0; i < 16; i++) todasCartas.Add(new Carta(TipoPeca.Peao, "Peão (move 1)", 1));
            for (int i = 0; i < 3; i++) todasCartas.Add(new Carta(TipoPeca.Cavalo, "Cavalo (move 4)", 4));
            for (int i = 0; i < 3; i++) todasCartas.Add(new Carta(TipoPeca.Torre, "Torre (move 6)", 6));
            for (int i = 0; i < 4; i++) todasCartas.Add(new Carta(TipoPeca.Bispo, "Bispo (move 8)", 8));
            for (int i = 0; i < 1; i++) todasCartas.Add(new Carta(TipoPeca.Dama, "Dama (move 12)", 12));

            // Embaralha a lista de cartas usando o algoritmo de Fisher-Yates (moderno).
            // Este é um método eficiente e imparcial para embaralhar uma coleção.
            int n = todasCartas.Count;
            while (n > 1)
            {
                n--;
                int k = randomInstance.Next(n + 1);
                // Troca a carta na posição 'k' com a carta na posição 'n'.
                (todasCartas[n], todasCartas[k]) = (todasCartas[k], todasCartas[n]);
            }

            // Converte a lista embaralhada em uma Fila (Queue) para formar o baralho de compra.
            baralho = new Queue<Carta>(todasCartas);
        }

        /// <summary>
        /// Ativa um evento aleatório quando um jogador cai em uma casa coringa.
        /// </summary>
        /// <param name="jogador">O jogador que ativou o efeito.</param>
        /// <param name="silent">Se verdadeiro, não imprime mensagens no console. Útil para simulações de IA.</param>
        public void AplicarEfeitoCoringa(Jogador jogador, bool silent = false)
        {
            if (!silent) Console.WriteLine($"\n!!! {jogador.Nome} caiu em uma casa coringa na posição {jogador.Posicao} !!!");

            // Escolhe um dos 5 tipos de efeito aleatoriamente.
            int tipoEfeito = randomInstance.Next(0, 5);

            switch (tipoEfeito)
            {
                case 0: // Azar: Voltar casas
                    int voltar = randomInstance.Next(1, 6);
                    jogador.Posicao = Math.Max(1, jogador.Posicao - voltar); // Math.Max garante que a posição não seja menor que 1.
                    if (!silent) Console.WriteLine($"Azar! Volte {voltar} casas. Nova posição: {jogador.Posicao}");
                    break;

                case 1: // Sorte: Avançar casas
                    int avancar = randomInstance.Next(1, 6);
                    jogador.Posicao = Math.Min(64, jogador.Posicao + avancar); // Math.Min garante que a posição não ultrapasse 64.
                    if (!silent) Console.WriteLine($"Sorte! Avance mais {avancar} casas. Nova posição: {jogador.Posicao}");
                    break;

                case 2: // Ladrão: Roubar carta
                    // Encontra o outro jogador. FirstOrDefault é seguro pois retorna null se não encontrar.
                    Jogador outroJogador = jogadores.FirstOrDefault(j => j != jogador);
                    if (outroJogador != null && outroJogador.Mao.Count > 0)
                    {
                        int cardIndexToSteal = randomInstance.Next(outroJogador.Mao.Count);
                        Carta cartaRoubada = outroJogador.Mao[cardIndexToSteal];
                        jogador.Mao.Add(cartaRoubada.Clone()); // Adiciona um clone da carta para evitar problemas de referência.
                        outroJogador.Mao.RemoveAt(cardIndexToSteal);
                        if (!silent) Console.WriteLine($"Roubou a carta '{cartaRoubada.Descricao}' de {outroJogador.Nome}!");
                    }
                    else
                    {
                        // Caso de fallback se não for possível roubar (ex: oponente sem cartas).
                        if (!silent) Console.WriteLine("Efeito Coringa: Tentou roubar carta, mas não foi possível.");
                    }
                    break;

                case 3: // Teleporte: Mover para uma casa aleatória
                    int novaPos = randomInstance.Next(1, 65);
                    jogador.Posicao = novaPos;
                    if (!silent) Console.WriteLine($"Teleporte para a posição {novaPos}!");
                    break;

                case 4: // Bloqueio: Pular a próxima vez
                    jogador.RodadasBloqueado = 1;
                    if (!silent) Console.WriteLine($"Bloqueado! Pulará a próxima rodada em que venceria o dado.");
                    break;
            }

            // Após o efeito coringa, verifica se o jogador atingiu a condição de vitória.
            if (jogador.Posicao >= 64)
            {
                jogador.Venceu = true;
                if (!silent) Console.WriteLine($"\n⭐ {jogador.Nome} alcançou a posição 64 (devido a coringa) e venceu o jogo! ⭐");
            }
        }

        /// <summary>
        /// Adiciona um novo jogador ao jogo e distribui suas cartas iniciais.
        /// </summary>
        /// <param name="nome">Nome do jogador.</param>
        /// <param name="isAI">Define se o jogador é controlado pela Inteligência Artificial.</param>
        public void AdicionarJogador(string nome, bool isAI = false)
        {
            if (jogadores.Count < 2)
            {
                var novoJogador = new Jogador(nome, isAI);
                jogadores.Add(novoJogador);
                Console.WriteLine($"Jogador {nome} {(isAI ? "[AI]" : "")} adicionado ao jogo.");

                // Distribui um conjunto fixo de cartas iniciais para cada jogador.
                novoJogador.ReceberCarta(new Carta(TipoPeca.Dama, "Dama (move 12)", 12), true);
                novoJogador.ReceberCarta(new Carta(TipoPeca.Torre, "Torre (move 6)", 6), true);
                novoJogador.ReceberCarta(new Carta(TipoPeca.Cavalo, "Cavalo (move 4)", 4), true);
                Console.WriteLine($"{nome} recebeu Dama, Torre e Cavalo iniciais.");
            }
            else
            {
                Console.WriteLine("O jogo já tem dois jogadores.");
            }
        }

        /// <summary>
        /// Exibe no console o status atual de todos os jogadores (posição, mão de cartas, etc.).
        /// </summary>
        public void MostrarStatusJogadores(bool silent = false)
        {
            if (silent) return; // Se for modo silencioso, simplesmente retorna.
            Console.WriteLine("\n=== Status dos Jogadores ===");
            foreach (var jogador in jogadores)
            {
                jogador.MostrarStatus();
            }
            Console.WriteLine("============================");
        }

        /// <summary>
        /// Permite que um jogador compre uma carta do baralho.
        /// Se o baralho estiver vazio, ele é re-inicializado e embaralhado automaticamente.
        /// </summary>
        /// <param name="jogador">O jogador que está comprando a carta.</param>
        /// <param name="silent">Se verdadeiro, a ação ocorre sem feedback no console.</param>
        public void ComprarCarta(Jogador jogador, bool silent = false)
        {
            // 1. Verifica se o baralho está vazio.
            if (baralho.Count == 0)
            {
                // 2. Informa ao usuário (se não estiver em modo silencioso).
                if (!silent)
                {
                    Console.WriteLine("\nO baralho acabou! Embaralhando um novo baralho...");
                }

                // 3. Re-inicializa o baralho com todas as cartas e as embaralha.
                InicializarBaralho();
            }

            // 4. Agora que o baralho com certeza tem cartas, entrega uma ao jogador.
            // Dequeue() remove e retorna o objeto no início da Queue.
            jogador.ReceberCarta(baralho.Dequeue(), silent);
        }

        /// <summary>
        /// Retorna uma lista de ações possíveis para um jogador.
        /// Essencial para a IA, que precisa saber quais jogadas pode avaliar.
        /// </summary>
        /// <param name="jogador">O jogador para o qual as ações serão listadas.</param>
        /// <returns>Uma lista de inteiros, onde cada número é o índice de uma carta na mão do jogador, e -1 representa "não jogar carta".</returns>
        public List<int> GetPossibleActionsForPlayer(Jogador jogador)
        {
            var actions = new List<int> { -1 }; // -1 representa a ação "passar a vez" ou "não jogar carta".
            for (int i = 0; i < jogador.Mao.Count; i++)
            {
                actions.Add(i); // Adiciona o índice de cada carta na mão como uma ação possível.
            }
            return actions;
        }

        /// <summary>
        /// Cria um "clone" profundo do estado atual do jogo.
        /// Isso é fundamental para algoritmos de IA (como MCTS), que precisam simular
        /// milhares de futuros possíveis sem alterar o estado real do jogo.
        /// </summary>
        /// <returns>Um novo objeto 'Jogo' que é uma cópia exata do atual.</returns>
        public Jogo Clone()
        {
            // Cria uma nova instância de Jogo sem inicializar baralho/coringas para evitar trabalho redundante.
            var clone = new Jogo(false);

            // Clona cada jogador e seus atributos (posição, mão de cartas, etc.).
            foreach (var jogador in this.jogadores) clone.jogadores.Add(jogador.Clone());

            // Clona o baralho, carta por carta.
            foreach (var carta in this.baralho) clone.baralho.Enqueue(carta.Clone());

            // Copia a lista de casas coringas.
            clone.casasCoringas = new List<int>(this.casasCoringas);

            // Copia o estado do jogo e o índice do jogador atual.
            clone.JogoAcabou = this.JogoAcabou;
            clone.CurrentPlayerIndex = this.CurrentPlayerIndex;

            return clone;
        }

        /// <summary>
        /// Verifica se o jogador ativo caiu na mesma casa de um oponente.
        /// Se sim, o oponente é "comido" e volta para o início.
        /// </summary>
        /// <param name="jogadorAtivo">O jogador que acabou de se mover.</param>
        /// <param name="silent">Se verdadeiro, a ação ocorre sem feedback no console.</param>
        public void VerificarColisaoEComer(Jogador jogadorAtivo, bool silent = false)
        {
            // A colisão não ocorre na casa 1 ou se o jogador já venceu.
            if (jogadorAtivo.Posicao <= 1 || jogadorAtivo.Venceu)
            {
                return;
            }

            // Encontra todos os outros jogadores que estão na mesma posição que o jogador ativo.
            // O .ToList() cria uma cópia, evitando problemas ao modificar a coleção original durante a iteração.
            var jogadoresComidos = jogadores.Where(j => j != jogadorAtivo && j.Posicao == jogadorAtivo.Posicao).ToList();

            foreach (var jogadorComido in jogadoresComidos)
            {
                if (!silent)
                {
                    Console.WriteLine($"\n!!! COLISÃO NA CASA {jogadorAtivo.Posicao} !!!");
                    Console.WriteLine($"A peça de {jogadorAtivo.Nome} comeu a peça de {jogadorComido.Nome}!");
                }

                // Envia o jogador "comido" de volta para a posição inicial (casa 1).
                jogadorComido.Posicao = 1;

                if (!silent)
                {
                    Console.WriteLine($"{jogadorComido.Nome} foi enviado de volta para a casa 1.");
                }
            }
        }
    }
}