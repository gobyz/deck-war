using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class FakeWarServer
{ 
    [SerializeField] private GameConfig gameConfig;

    private GameState gameState;

    private List<Player> players = new List<Player>();

    private Client client; // only one client because this is a fake server, in a real server this would be a list of clients

    private CancellationTokenSource cts = new();

    public static event Action<GameState> OnGameStateSet;

    private List<CardData> winPile = new List<CardData>(); // pile of cards won in the round

    private bool isWar = false; // flag to indicate if the last round was a war, used to determine how many cards to draw in the next round

    public int cardsToDraw = 1;

    public FakeWarServer(Client client)
    {
        this.client = client;
        gameConfig = GameConfigProvider.Instance.GameConfig;
        StartServer().Forget();
    }

    private async UniTaskVoid StartServer()
    {
        await Run(cts.Token);
    }

    private async UniTask Run(CancellationToken token)
    {
        await UniTask.Delay(1000); // simulate server startup time

        Debug.Log("Server started. Waiting for players to join...");

        SetGameState(GameState.WaitingForPlayers);

        Join(new JoinRequest { ClientId = gameConfig.ClientIdPlayer }).Forget(); // simulate fake player joining, in a real server this would be triggered by a client connecting and sending a join request
        
        await AreAllPlayersReady();

        Debug.Log("All players ready. Starting game...");

        SetGameState(GameState.Ready);

        await UniTask.Delay(1000);

        InitializePlayerDecks();

        SetGameState(GameState.Shuffling);
        
        await UniTask.Delay(1000); //shuffling

        await Game(token);      
    }

    private async UniTask Game(CancellationToken token)
    {
        while(gameState != GameState.GameOver)
        {
            Debug.Log("Starting round...");        

            Debug.Log("Waiting for players to draw...");

            SetCardsToDraw();
            
            SetGameState(GameState.Drawing);  

            await Draw();

            Debug.Log("All players drawn. Resolving round...");

            SetGameState(GameState.Resolving);

            await UniTask.Delay(500); // resolving

            ResolveResponse resolveResponse = await Resolve();

            Debug.Log("Round resolved. Sending response...");

            SendResolveResponse(resolveResponse);

            if(players.Any(p => !p.CanDraw()))
            {
                GameOver().Forget();
            }
        }
    }

    private async UniTaskVoid GameOver()
    {
        SetGameState(GameState.GameOver);

        Debug.Log("Game over.");
    }

    private void SetCardsToDraw()
    {
        cardsToDraw = isWar ? 3 : 1;
    }


    public async UniTask<JoinResponse> Join(JoinRequest joinRequest)
    {
        await UniTask.Delay(500); // simulate server latency

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

    private async UniTask Draw()
    {
        for (int i = 0; i < cardsToDraw; i++)
        {
             FakePlayerDraw().Forget(); // simulate fake player drawing, in a real server this would be triggered by a client sending a draw request
        }

        while (!players.All(p => p.DrawnCardsCount == cardsToDraw))
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

    public async UniTask<DrawResponse> DrawCard(DrawRequest drawRequest)
    {
        await UniTask.Delay(500); // simulate server latency

        Player player = GetPlayer(drawRequest.PlayerId);

        if(player.DrawnCardsCount >= cardsToDraw)
        {
            return new DrawResponse
            {
                Status = DrawResponseStatus.AlreadyDrawn
            };
        }

        if(!player.CanDraw())
        {
            Debug.LogError($"Player {player.PlayerID} cannot draw, no cards left in deck or war deck.");

            cts.Cancel(); // end game if a player cannot draw, in a real server this would be handled more gracefully

            GameOver().Forget();

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
            PlayerCardId = card.Id,
            DeckInfo = new DeckData
            {
                DeckCardsLeft = player.Deck.CardsLeft,
                WarDeckCardsLeft = player.WarDeck.CardsLeft
            },
            Status = DrawResponseStatus.Success
        };
    }

    private async UniTaskVoid FakePlayerDraw()
    {
        await UniTask.Delay(UnityEngine.Random.Range(300, 800));

        DrawResponse drawResponse = await DrawCard(new DrawRequest { PlayerId = 0 });

        client.OnEnemyDrawn(drawResponse);
    }

    private int GetResult(List<CardData> a, List<CardData> b)
    {
        int result = 0;

        if(a.Count == b.Count)
        {
            for(int i = 0; i < a.Count; i++)
            {
                if (a[i].Value > b[i].Value)
                {
                    result -= 1;
                }
                else if (a[i].Value < b[i].Value)
                {
                    result += 1;
                }
            }
        }
        else
        {
            Debug.LogError("Error: Players have drawn different number of cards. This should not happen.");
        }

        return result; // return positive if player 1 wins, negative if player 2 wins, 0 if tie
    }

    private async UniTask<ResolveResponse> Resolve()
    {
        await UniTask.Delay(500); // simulate server latency

        Player player1 = players[0];
        Player player2 = players[1];

        int result = GetResult(player1.DrawnCards, player2.DrawnCards);

        if(result != 0)
        {
            isWar = false; 

            int winner = result < 0 ? 0 : 1; // determine winner based on result

            Player winnerPlayer = players[winner]; 

            winPile.ForEach(card => winnerPlayer.WarDeck.AddCard(card)); // winner takes all cards in the win pile

            winPile.Clear(); // clear win pile for next round

            players.ForEach(p => p.DrawnCards.Clear()); // clear drawn cards for all players

            return new ResolveResponse
            {
                WinnerId = players[winner].PlayerID,
                WonPileCardsCount = winPile.Count,
                DeckDatas = GetDeckDatas(),
                IsATie = false,       
            };
        }
        else
        {
            isWar = true; 

            players.ForEach(p => p.DrawnCards.Clear()); // clear drawn cards for all players

            return new ResolveResponse
            {
                WonPileCardsCount = winPile.Count,
                DeckDatas = GetDeckDatas(),
                IsATie = true
            };
        }        
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

    private void SendResolveResponse(ResolveResponse response)
    {
        client.ReceiveResolveResponce(response);
    }

     private void SetGameState(GameState newState)
    {
        gameState = newState;

        OnGameStateSet?.Invoke(gameState);

        Debug.Log("Game state set to: " + gameState);
    }
}

public enum GameState
    {
        Undefined,
        WaitingForPlayers,
        Ready,
        Shuffling,
        Drawing,
        Resolving,
        GameOver
    }

