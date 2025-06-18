using System;
using System.Diagnostics; // Necessário para usar a classe Stopwatch para medir o tempo.
using System.Linq; // Necessário para métodos de consulta como .All(), .Any(), .FirstOrDefault().

namespace JogoChamelionKingAppConsoleV2
{
    /// <summary>
    /// A classe principal do programa. Contém o método Main, que é o ponto de entrada da aplicação.
    /// Ela gerencia o loop principal do jogo, a interação com o usuário no console e a orquestração
    /// de todas as outras classes (Jogo, Jogador, MCTS_AI_Player).
    /// </summary>
    class Program
    {
        /// <summary>
        /// O ponto de entrada da aplicação.
        /// </summary>
        /// <param name="args">Argumentos de linha de comando (não utilizados neste programa).</param>
        static void Main(string[] args)
        {
            // Garante que caracteres especiais (como acentos e emojis) sejam exibidos corretamente no console.
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Bem-vindo ao Jogo dos Reis Camaleões!");
            Console.WriteLine("Objetivo: Ser o primeiro a alcançar ou ultrapassar a casa 64 no tabuleiro.\n");

            // --- Inicialização de Objetos Principais ---
            Random mainRandom = new Random(); // Gerador de números aleatórios para os dados.
            var jogo = new Jogo(); // Cria a instância principal do jogo, que gerencia o estado e as regras.
            MCTS_AI_Player aiPlayer = null; // A instância da IA, inicializada como nula. Só será criada se o jogador 2 for uma IA.
            int aiPlayerIndex = -1; // O índice (0 ou 1) que a IA ocupará na lista de jogadores.

            // --- Configuração dos Jogadores ---
            // Coleta de informações para configurar os dois jogadores da partida.
            Console.Write("Digite o nome do Jogador 1: ");
            string p1Name = Console.ReadLine();
            jogo.AdicionarJogador(p1Name);

            Console.Write("Jogador 2 é AI? (s/n): ");
            bool player2IsAI = Console.ReadLine().ToLower() == "s";
            if (player2IsAI)
            {
                // Se o jogador 2 for uma IA, ele é adicionado com um nome padrão e a flag 'isAI' como verdadeira.
                jogo.AdicionarJogador("Computador MCTS", true);
                aiPlayerIndex = 1; // A IA será o jogador de índice 1.
                aiPlayer = new MCTS_AI_Player(aiPlayerIndex); // Cria a instância do cérebro da IA.
            }
            else
            {
                // Se o jogador 2 for humano, pede seu nome.
                Console.Write("Digite o nome do Jogador 2: ");
                string p2Name = Console.ReadLine();
                jogo.AdicionarJogador(p2Name);
            }

            // Inicia um cronômetro para medir a duração total da partida.
            Stopwatch gameStopwatch = new Stopwatch();
            gameStopwatch.Start();

            Console.WriteLine("\nO jogo começou! Boa sorte aos jogadores.");

            // --- Game Loop Principal ---
            // O coração do jogo. Este loop continuará executando rodada após rodada até que a condição de fim de jogo seja atingida.
            int roundNumber = 0;
            while (!jogo.JogoAcabou)
            {
                roundNumber++;
                Console.WriteLine($"\n--- Rodada {roundNumber} ---");
                jogo.MostrarStatusJogadores(); // Exibe o estado atual de cada jogador.

                // Condição de empate por inatividade (stalemate) para evitar jogos infinitos.
                if (jogo.jogadores.All(j => j.Posicao == 1 && j.Mao.Count == 0) && roundNumber > 100)
                {
                    Console.WriteLine("Jogo empatado por inatividade após 100 rodadas. Encerrando.");
                    break;
                }

                // Pausa para o usuário poder acompanhar o ritmo do jogo.
                Console.WriteLine("\nPressione Enter para rolar os dados...");
                Console.ReadLine();

                // --- Disputa de Dados ---
                // Determina qual jogador terá a vez na rodada.
                int dadoJogador1 = mainRandom.Next(1, 7);
                int dadoJogador2 = mainRandom.Next(1, 7);

                Console.WriteLine($"\n{jogo.jogadores[0].Nome} rolou {dadoJogador1}");
                Console.WriteLine($"{jogo.jogadores[1].Nome} rolou {dadoJogador2}");

                Jogador vencedorRodada = null;
                int vencedorRodadaIndex = -1;
                bool empate = false;

                if (dadoJogador1 > dadoJogador2)
                {
                    vencedorRodada = jogo.jogadores[0];
                    vencedorRodadaIndex = 0;
                }
                else if (dadoJogador2 > dadoJogador1)
                {
                    vencedorRodada = jogo.jogadores[1];
                    vencedorRodadaIndex = 1;
                }
                else // Empate
                {
                    empate = true;
                    Console.WriteLine("\nEmpate na disputa de dados! Ambos os jogadores podem jogar uma carta.");
                }

                // --- Lógica de Turno: Caso de Empate ---
                if (empate)
                {
                    // Se houve empate nos dados, ambos os jogadores têm a oportunidade de jogar.
                    foreach (var jogadorAtual in jogo.jogadores)
                    {
                        Console.WriteLine($"\n--- Vez de {jogadorAtual.Nome} (devido ao empate) ---");

                        // Verifica se o jogador está bloqueado por um efeito coringa.
                        if (jogadorAtual.RodadasBloqueado > 0)
                        {
                            Console.WriteLine($"{jogadorAtual.Nome} está bloqueado e não pode agir! Perde a vez.");
                            jogadorAtual.RodadasBloqueado--;
                            continue; // Pula para o próximo jogador no loop.
                        }

                        // Define o jogador atual no objeto 'jogo' para que a IA saiba para quem está decidindo.
                        jogo.CurrentPlayerIndex = jogo.jogadores.IndexOf(jogadorAtual);
                        jogo.ComprarCarta(jogadorAtual); // No empate, ambos os jogadores compram uma carta.

                        int cardToPlay = -1; // -1 significa "não jogar carta".

                        // --- Decisão do Jogador (IA ou Humano) ---
                        if (jogadorAtual.IsAI && aiPlayer != null)
                        {
                            // Bloco de decisão da IA.
                            Console.WriteLine($"{jogadorAtual.Nome} [AI] está pensando...");
                            Jogo stateForMCTS = jogo.Clone(); // Clona o estado do jogo para a IA poder simular sem alterar o jogo real.

                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            cardToPlay = aiPlayer.GetBestAction(stateForMCTS, 2000); // Executa o MCTS por 2000 iterações.
                            stopwatch.Stop();
                            Console.WriteLine($"Tempo de pensamento da IA: {stopwatch.ElapsedMilliseconds} ms");

                            // Comunica a decisão da IA.
                            if (cardToPlay == -1) Console.WriteLine($"{jogadorAtual.Nome} [AI] decidiu não jogar nenhuma carta.");
                            else if (cardToPlay >= 0 && cardToPlay < jogadorAtual.Mao.Count) Console.WriteLine($"{jogadorAtual.Nome} [AI] decidiu jogar a carta: {jogadorAtual.Mao[cardToPlay].Descricao}");
                            else { cardToPlay = -1; } // Segurança: se a IA retornar um índice inválido, não joga.
                        }
                        else if (!jogadorAtual.IsAI)
                        {
                            // Bloco de decisão do jogador humano.
                            // Mostra a mão e pede para o jogador escolher uma carta.
                            // ... (código de interação com o usuário)
                        }

                        // --- Execução da Ação ---
                        // Este bloco é executado tanto para a decisão da IA quanto para a do jogador humano.
                        if (cardToPlay != -1)
                        {
                            var cartaUsada = jogadorAtual.Mao[cardToPlay];
                            jogadorAtual.TransformarPeca(cartaUsada.Tipo);
                            jogadorAtual.Mao.RemoveAt(cardToPlay);
                        }

                        // Move o jogador e aplica os efeitos de colisão/coringa.
                        jogadorAtual.Mover(jogo.casasCoringas, (j, s) => jogo.AplicarEfeitoCoringa(j, s));
                        jogo.VerificarColisaoEComer(jogadorAtual);

                        // Verifica se o jogador venceu após sua jogada.
                        if (jogadorAtual.Venceu)
                        {
                            jogo.JogoAcabou = true;
                            break; // Interrompe o loop do empate, pois o jogo terminou.
                        }
                    }
                }
                // --- Lógica de Turno: Caso de Vencedor Único ---
                else
                {
                    // Se um jogador venceu a disputa de dados, apenas ele joga.
                    jogo.CurrentPlayerIndex = vencedorRodadaIndex;

                    if (vencedorRodada.RodadasBloqueado > 0)
                    {
                        Console.WriteLine($"{vencedorRodada.Nome} está bloqueado e não pode agir! Perde a vez.");
                        vencedorRodada.RodadasBloqueado--;
                    }
                    else
                    {
                        Console.WriteLine($"\n{vencedorRodada.Nome} venceu a disputa de dados!");
                        jogo.ComprarCarta(vencedorRodada);

                        int cardToPlay = -1;

                        // O bloco de decisão (IA vs. Humano) é idêntico ao do caso de empate.
                        if (vencedorRodada.IsAI && aiPlayer != null)
                        {
                            // ... (código da IA)
                            Console.WriteLine($"{vencedorRodada.Nome} [AI] está pensando...");
                            Jogo stateForMCTS = jogo.Clone();
                            Stopwatch stopwatch = new Stopwatch();
                            stopwatch.Start();
                            cardToPlay = aiPlayer.GetBestAction(stateForMCTS, 1000);
                            stopwatch.Stop();
                            Console.WriteLine($"Tempo de pensamento da IA: {stopwatch.ElapsedMilliseconds} ms");

                            if (cardToPlay == -1) Console.WriteLine($"{vencedorRodada.Nome} [AI] decidiu não jogar nenhuma carta.");
                            else if (cardToPlay >= 0 && cardToPlay < vencedorRodada.Mao.Count) Console.WriteLine($"{vencedorRodada.Nome} [AI] decidiu jogar a carta: {vencedorRodada.Mao[cardToPlay].Descricao}");
                            else { cardToPlay = -1; }
                        }
                        else if (!vencedorRodada.IsAI)
                        {
                            Console.WriteLine($"\n--- Sua vez, {vencedorRodada.Nome}! ---");
                            Console.WriteLine($"Peça atual: {vencedorRodada.PecaAtual}");
                            Console.WriteLine("Cartas na mão:");
                            if (vencedorRodada.Mao.Any())
                            {
                                for (int i = 0; i < vencedorRodada.Mao.Count; i++)
                                {
                                    Console.WriteLine($"{i + 1}. {vencedorRodada.Mao[i].Descricao}");
                                }
                                Console.Write("Deseja usar alguma carta para se transformar? (s/n): ");
                                if (Console.ReadLine().ToLower() == "s")
                                {
                                    bool validChoice = false;
                                    while (!validChoice)
                                    {
                                        Console.Write("Digite o número da carta que deseja usar: ");
                                        if (int.TryParse(Console.ReadLine(), out int escolhaNum) && escolhaNum > 0 && escolhaNum <= vencedorRodada.Mao.Count)
                                        {
                                            cardToPlay = escolhaNum - 1;
                                            validChoice = true;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Escolha inválida.");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("Você não tem cartas na mão para usar.");
                            }
                        }

                        // O bloco de execução da ação também é idêntico.
                        if (cardToPlay != -1)
                        {
                            var cartaUsada = vencedorRodada.Mao[cardToPlay];
                            vencedorRodada.TransformarPeca(cartaUsada.Tipo);
                            vencedorRodada.Mao.RemoveAt(cardToPlay);
                        }

                        vencedorRodada.Mover(jogo.casasCoringas, (j, s) => jogo.AplicarEfeitoCoringa(j, s));
                        jogo.VerificarColisaoEComer(vencedorRodada);

                        if (vencedorRodada.Venceu)
                        {
                            jogo.JogoAcabou = true;
                        }
                    }
                }

                // --- Fim da Rodada ---
                // Se o jogo não acabou, as peças de todos os jogadores são resetadas para o Rei para a próxima rodada.
                if (!jogo.JogoAcabou)
                {
                    foreach (var jogador in jogo.jogadores)
                    {
                        jogador.ResetarPeca();
                    }
                }

                if (jogo.JogoAcabou) break; // Garante que o loop principal termine imediatamente.
            }

            gameStopwatch.Stop(); // Para o cronômetro da partida.

            // Formata o tempo total de jogo para um formato legível.
            TimeSpan ts = gameStopwatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);

            // --- Fim de Jogo ---
            // Exibe o status final e a mensagem de vitória.
            jogo.MostrarStatusJogadores();
            Console.WriteLine($"\nTempo total de partida: {elapsedTime}");
            Console.WriteLine("\nFim do jogo!");

            Jogador ganhadorFinal = jogo.jogadores.FirstOrDefault(j => j.Venceu);
            if (ganhadorFinal != null)
            {
                Console.WriteLine($"🎉 {ganhadorFinal.Nome} é o VENCEDOR! 🎉");
            }
            else
            {
                Console.WriteLine("O jogo terminou sem um vencedor claro.");
            }
        }
    }
}