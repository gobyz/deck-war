using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Server
{ 
    private GameConfig gameConfig;
    private GameState gameState;
    private Client client; // only one client because this is a fake server, in a real server this would be a list of clients
    private List<Player> players = new List<Player>(); 
    private List<CardData> winPile = new List<CardData>();
    private bool isWar = false; // flag to indicate if the round is a war, used to determine how many cards to draw in the next round
    private int cardsToDraw = 1; // number of cards to draw in the next round
    private bool isGameOver = false;
    private Outcome gameOutcome; 
    private CancellationTokenSource cts = new();

    public Server(Client client)
    {
        this.client = client;
        gameConfig = GameConfigProvider.Instance.GameConfig;
        StartServerLoop();
    }

    private void StartServerLoop()
    {
        cts = new();

        ServerLoop(cts.Token).Forget();
    }

    private async UniTask ServerLoop(CancellationToken token)
    {
        ServerClear();

        await RunLobbyPhase(token);

        await RunGameStartPhase(token);

        await RunGameplayPhase(token);

        await RunGameOverPhase(token);
    }

    private async UniTask RunLobbyPhase(CancellationToken token)
    {
        Debug.Log("Starting server...");

        await UniTask.Delay(1000, cancellationToken: token);

        SetGameState(GameState.WaitingForPlayers);

        Debug.Log("Waiting for players to join...");

        // Fake enemy join
        Join(new JoinRequest{ ClientId = gameConfig.ClientIdEnemy }, 0).Forget();

        await AreAllPlayersReady();
    }

    private async UniTask RunGameStartPhase(CancellationToken token)
    {
        Debug.Log("All players ready.");

        await UniTask.Delay(1000, cancellationToken: token);

        SetGameState(GameState.Shuffling);

        Debug.Log("Shuffling cards...");

        await UniTask.Delay(3000, cancellationToken: token);

        InitializePlayerDecks();

        SetGameState(GameState.Ready);

        Debug.Log("Game ready.");
    }

     private async UniTask RunGameplayPhase(CancellationToken token)
    {
        while (!isGameOver)
        {
            await RunRound(token);
        }
    }
    private async UniTask RunRound(CancellationToken token)
    {
        Debug.Log("Starting new round...");

        await RunDrawPhase(token);

        if (!isGameOver)
        {
            await RunResolvePhase(token); 

            isGameOver = EvaluateGameOver();
        }    
    }

    private async UniTask RunDrawPhase(CancellationToken token)
    {
        Debug.Log("Waiting for players to draw...");

        SetCardsToDraw();

        SetGameState(GameState.Drawing);

        await Draw();
    }

    private async UniTask RunResolvePhase(CancellationToken token)
    {
        Debug.Log("Resolving round...");

        SetGameState(GameState.Resolving);

        await UniTask.Delay(500, cancellationToken: token);

        Resolve resolve = await Resolve();

        SendResolve(resolve);

        Debug.Log("Round resolved.");
    }

    private async UniTask RunGameOverPhase(CancellationToken token)
    {
        SetGameState(GameState.GameOver);

        await GameOver();
    }

    private async UniTask GameOver()
    {
        SetGameState(GameState.GameOver);

        SendGameOver();

        Debug.Log($"Game over. Game Outcome: {gameOutcome.ToString()}");

        await UniTask.Delay(3000);

        StartServerLoop(); // restart server loop 
    }

    private bool EvaluateGameOver()
    {
        if (players.All(p => p.CanDraw()))
        {
            return false;
        }

        if (players.All(p => !p.CanDraw()))
        {
            gameOutcome = Outcome.Tie;
        }
        else
        {
            Player winner = players.First(p => p.CanDraw());

            gameOutcome = GetWinnerGameOutcome(winner.PlayerID);
        }

        return true;
    }

    private void ServerClear()
    {
        players.Clear();
        winPile.Clear();
        isWar = false;
        cardsToDraw = 1;
        gameOutcome = Outcome.Undefined;
        isGameOver = false;
    }

    private void SetCardsToDraw()
    {
        cardsToDraw = isWar ? gameConfig.WarCardsCount : 1;
    }

    public async UniTask<JoinResponse> Join(JoinRequest joinRequest, int delay = 500)
    {
        await UniTask.Delay(delay); // simulate server latency
        
        //emulate server errors
        if(joinRequest.ClientId == gameConfig.ClientIdPlayer)
        {
            if(UnityEngine.Random.Range(0f, 100f) <= gameConfig.JoinTimeoutChance)
            {
                await UniTask.Delay(gameConfig.ServerTimeoutDuration);
            }

            if(UnityEngine.Random.Range(0f, 100f) <= gameConfig.JoinErrorChance)
            {
                return new JoinResponse
                {
                    Status = JoinResponseStatus.Error
                };
            }   
            else if(UnityEngine.Random.Range(0f, 100f) <= gameConfig.ServerFullChance)
            {
                return new JoinResponse
                {
                    Status = JoinResponseStatus.ServerFull
                };
            }   
        }

        if(players.Count < gameConfig.MaxPlayers)
        {
            Player player = new Player(joinRequest.ClientId);

            players.Add(player);

            player.State = PlayerState.Ready;

            player.PlayerID = players.Count - 1; // assign player ID based on order of joining, in a real server this would be more robust
           
            Debug.Log("Player " + joinRequest.ClientId + " joined the game. PlayerId assigned: " + player.PlayerID);

            return new JoinResponse
            {
                PlayerId = player.PlayerID,
                Status = JoinResponseStatus.Success
            };
        }
        else
        {
            Debug.Log("Player " + joinRequest.ClientId + " failed to join the game. Max players reached.");
            
            return new JoinResponse
            {
                Status = JoinResponseStatus.ServerFull
            };
        }        
    }

    private async UniTask AreAllPlayersReady()
    {
        while (players.Count < gameConfig.MaxPlayers || !players.All(p => p.State == PlayerState.Ready))
        {
            await UniTask.Yield();
        }
    }

    private void InitializePlayerDecks()
    {
        Deck deck = new Deck(gameConfig.FullDeck);
        deck.Shuffle();

        foreach (Player player in players)
        {
            player.InitializeDecks();
        }

        for (int i = 0; i < gameConfig.StartingDeckSize; i++)
        {
            players.ForEach(p => p.Deck.AddCard(deck.Draw()));
        }
    }

    private async UniTask Draw()
    {
        while (!isGameOver && !players.All(p => p.DrawnCardsCount == cardsToDraw))
        {
            await UniTask.Yield();
        }
    }
   
    public async UniTask RequestDraw(DrawRequest drawRequest)
    {
        await UniTask.Delay(500);

        if(UnityEngine.Random.Range(0f, 100f) <= gameConfig.DrawTimeoutChance)
        {
            await UniTask.Delay(gameConfig.ServerTimeoutDuration);
        }

        //emulate server errors
        if(UnityEngine.Random.Range(0f, 100f) <= gameConfig.DrawErrorChance)
        {
            DrawResponse drawResponse = new DrawResponse
            {
                Status = DrawResponseStatus.Error
            };

            client.ReceiveDrawn(drawResponse);

            return;
        }  

        bool isGameOverEvaluation = EvaluateGameOver();

        DrawResponse enemyDrawResponse = DrawCard(new DrawRequest { PlayerId = 0 });

        DrawResponse playerDrawResponse = DrawCard(drawRequest); 

        isGameOver = isGameOverEvaluation;

        client.ReceiveDrawn(enemyDrawResponse); 

        client.ReceiveDrawn(playerDrawResponse);
    }

    public DrawResponse DrawCard(DrawRequest drawRequest)
    {
        if(gameState != GameState.Drawing)
        {
            Debug.LogError($"Game is not in drawing phase. This should not happen. Game state: {gameState}");

            return new DrawResponse
            {
                Status = DrawResponseStatus.Error
            };
        }

        Player player = GetPlayer(drawRequest.PlayerId);   

        if(player.DrawnCardsCount >= cardsToDraw)
        {
            Debug.Log($"Player {player.PlayerID} has already drawn {cardsToDraw} cards.");

            return new DrawResponse
            {
                Status = DrawResponseStatus.AlreadyDrawn
            };
        }

        if(!player.CanDraw())
        {
            Debug.Log($"Player {player.PlayerID} cannot draw, no cards left in deck or war deck.");
        
            return new DrawResponse
            {
                Status = DrawResponseStatus.NoCardsLeft
            };
        }

        CardData card = player.Draw();

        player.DrawnCards.Add(card);

        winPile.Add(card);

        Debug.Log($"Player {player.PlayerID} drew card: {card.Id}");

        return new DrawResponse
        {
            PlayerId = drawRequest.PlayerId,
            CardId = card.Id,
            IsWar = isWar,
            DeckInfo = new DeckData
            {
                DeckCardsLeft = player.Deck.CardsLeft,
                WarDeckCardsLeft = player.WarDeck.CardsLeft
            },
            Status = DrawResponseStatus.Success
        };
    }

    private Outcome GetResult(CardData a, CardData b)
    {
        Outcome outcome = Outcome.Tie;

        if (a.Value > b.Value)
        {
            outcome = Outcome.EnemyWin;
        }
        else if (a.Value < b.Value)
        {
            outcome = Outcome.PlayerWin;
        }

        return outcome;
    }

    private async UniTask<Resolve> Resolve()
    {
        await UniTask.Delay(500); // simulate server latency

        Player enemy = players[0];
        Player player = players[1];

        Outcome roundOutcome = GetResult(enemy.DrawnCards.Last(), player.DrawnCards.Last());

        if(roundOutcome == Outcome.Undefined)
        {
            Debug.LogError("Game outcome is undefined. This should not happen.");

            return null;
        }

        isWar = roundOutcome == Outcome.Tie;

        Player winnerPlayer = roundOutcome == Outcome.EnemyWin ? enemy : player;

        Resolve resolve = new Resolve
        {
            WinnerId = winnerPlayer.PlayerID,
            WonPileCardsCount = winPile.Count,
            DeckDatas = GetDeckDatas(),
            Outcome = roundOutcome
        };

        if(!isWar)
        {
            winPile.ForEach(card => winnerPlayer.WarDeck.AddCard(card)); // winner takes all cards in the win pile

            winPile.Clear(); // clear win pile for next round

            ClearDrawnCards(); // clear drawn cards for all players
        }

        return resolve;
    }

    private void ClearDrawnCards()
    {
        players.ForEach(p => p.DrawnCards.Clear()); 
    }

    private Player GetPlayer(int playerId)
    {
        return players.FirstOrDefault(p => p.PlayerID == playerId);
    }

    private List<DeckData> GetDeckDatas()
    {
        return players.Select(p => new DeckData
        {
            PlayerId = p.PlayerID,
            DeckCardsLeft = p.Deck.CardsLeft,
            WarDeckCardsLeft = p.WarDeck.CardsLeft
        }).ToList();
    }

    private void SendResolve(Resolve resolve)
    {
        client.ReceiveResolve(resolve);
    }

    private void SendGameOver()
    {
        client.ReceiveGameOver(new GameOver{ GameOutcome = gameOutcome });
    }

     private void SetGameState(GameState newState)
    {
        gameState = newState;

        client.ReceiveGameState(gameState);   
    }

    private Outcome GetWinnerGameOutcome(int winnerId)
    {
        if(winnerId == 1)
        {
            return Outcome.PlayerWin;
        }
        else
        {
            return Outcome.EnemyWin;
        }
    }
}