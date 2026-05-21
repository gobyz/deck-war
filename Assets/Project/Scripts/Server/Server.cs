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

    private int gameWinnerId = -1; // id of the player who won the game

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

        await UniTask.Delay(1000); // simulate delay

        Debug.Log("Server started. Waiting for players to join...");

        SetGameState(GameState.WaitingForPlayers);

        Join(new JoinRequest { ClientId = gameConfig.ClientIdEnemy }, 0).Forget(); // simulate fake player joining
        
        await AreAllPlayersReady();

        Debug.Log("All players ready. Starting game...");

        await UniTask.Delay(1000); // simulate delay

        SetGameState(GameState.Shuffling);
        
        await UniTask.Delay(3000); // simulate delay

        SetGameState(GameState.Ready);

        InitializePlayerDecks();

        await Game(token);      
    }

    private async UniTask Game(CancellationToken token)
    {
        while (true)
        {
            Debug.Log("Starting round...");        

            Debug.Log("Waiting for players to draw...");

            SetCardsToDraw();

            SetGameState(GameState.Drawing);  

            await Draw();

            token.ThrowIfCancellationRequested();

            Debug.Log("All players drawn. Resolving round...");

            SetGameState(GameState.Resolving);

            await UniTask.Delay(500); // resolving

            Resolve resolve = await Resolve();

            Debug.Log("Round resolved. Sending response...");

            SendResolve(resolve);

            if(players.Any(p => !p.CanDraw()))
            {
                gameWinnerId = GetWinnerId(players.Find(p => !p.CanDraw()).PlayerID);

                GameOver().Forget();

                cts.Cancel();

                token.ThrowIfCancellationRequested();
            }   
        }
    }

    private async UniTaskVoid GameOver()
    {
        SetGameState(GameState.GameOver);

        SendGameOver();

        Debug.Log("Game over.");

        await UniTask.Delay(3000);

        StartServerLoop(); // restart server loop 
    }

    private void ServerClear()
    {
        players.Clear();
        winPile.Clear();
        isWar = false;
        cardsToDraw = 1;
        gameWinnerId = -1;
    }

    private void SetCardsToDraw()
    {
        cardsToDraw = isWar ? gameConfig.WarCardsCount : 1;
    }

    public async UniTask<JoinResponse> Join(JoinRequest joinRequest, int delay = 500)
    {
        await UniTask.Delay(delay); // simulate server latency

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
            await FakePlayerDraw(); // simulate fake player drawing, in a real server this would be triggered by a client sending a draw request
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

        if(gameState != GameState.Drawing)
        {
            return new DrawResponse
            {
                Status = DrawResponseStatus.Error
            };
        }

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
        
            gameWinnerId = GetWinnerId(player.PlayerID);

            GameOver().Forget(); // end game if a player cannot draw, in a real server this would be handled more gracefully

            cts.Cancel();

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

    private async UniTask FakePlayerDraw()
    {
        await UniTask.Delay(UnityEngine.Random.Range(300, 800));

        DrawResponse drawResponse = await DrawCard(new DrawRequest { PlayerId = 0 });

        if (drawResponse.Status == DrawResponseStatus.Success)
        {
            client.ReceiveEnemyDrawn(drawResponse);
        }   
    }

    private int GetResult(CardData a, CardData b)
    {
        int result = -1;

        if (a.Value > b.Value)
        {
            result = 0;
        }
        else if (a.Value < b.Value)
        {
            result = 1;
        }

        return result;
    }

    private async UniTask<Resolve> Resolve()
    {
        await UniTask.Delay(500); // simulate server latency

        Player player1 = players[0];
        Player player2 = players[1];

        int result = GetResult(player1.DrawnCards.Last(), player2.DrawnCards.Last());

        if(result != -1)
        {
            isWar = false; 

            int winner = result > 0 ? 1 : 0; // determine winner based on result

            Player winnerPlayer = players[winner]; 

            winPile.ForEach(card => winnerPlayer.WarDeck.AddCard(card)); // winner takes all cards in the win pile

            winPile.Clear(); // clear win pile for next round

            players.ForEach(p => p.DrawnCards.Clear()); // clear drawn cards for all players

            return new Resolve
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

            return new Resolve
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

    private void SendResolve(Resolve resolve)
    {
        client.ReceiveResolve(resolve);
    }

    private void SendGameOver()
    {
        client.ReceiveGameOver(new GameOver{ WinnerId = gameWinnerId });
    }

     private void SetGameState(GameState newState)
    {
        gameState = newState;

        client.ReceiveGameState(gameState);   
    }

    private int GetWinnerId(int loserId)
    {
        return players.FirstOrDefault(p => p.PlayerID != loserId).PlayerID;
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

