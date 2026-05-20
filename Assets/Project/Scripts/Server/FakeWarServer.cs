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

    private GameState state;

    private List<Player> players = new List<Player>();

    private Client client; // only one client because this is a fake server, in a real server this would be a list of clients

    private CancellationTokenSource cts = new();

    public static event Action<GameState> OnGameStateChanged;


    public async UniTask<JoinResponse> Join(JoinRequest joinRequest, Client client = null)
    {
        await UniTask.Delay(500); // simulate server latency

        if(players.Count < gameConfig.MaxPlayers)
        {
            Player player = new Player(joinRequest.ClientId);

            players.Add(player);

            player.State = PlayerState.Ready;

            player.PlayerID = players.Count - 1; // assign player ID based on order of joining, in a real server this would be more robust

            if (client != null)
            {
                 this.client = client;
            }
           
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

    private async UniTask CheckAllPlayersReady()
    {
        while (players.Count != gameConfig.MaxPlayers || !players.All(p => p.State == PlayerState.Ready))
        {
            await UniTask.Yield();
        }
    }

    private async UniTask CheckAllPlayersDrawn()
    {
        while (players.Count != gameConfig.MaxPlayers || !players.All(p => p.State == PlayerState.Drawn))
        {
            await UniTask.Yield();
        }
    }

    public FakeWarServer()
    {
        gameConfig = GameConfigProvider.Instance.GameConfig;
        StartServer().Forget();
    }

    private async UniTaskVoid StartServer()
    {
        await Run(cts.Token);
    }

    private void InitializePlayerDecks()
    {
        Deck deck = new Deck(gameConfig.FullDeck);
        deck.Shuffle();

        foreach (Player player in players)
        {
            player.InitializeDecks();
        }

        for (int i = 0; i < 26; i++)
        {
            players.ForEach(p => p.Deck.AddCard(deck.Draw()));
        }
    }

    private int Resolve(CardData a, CardData b)
    {
        if (a.Value > b.Value) 
        { 
            return 0; 
        }
        if (a.Value < b.Value) 
        { 
            return 1; 
        } 

        return -1; // tie
    }

    private async UniTask Run(CancellationToken token)
    {
        // click join

        Debug.Log("Server started. Waiting for players to join...");

        SetState(GameState.WaitingForPlayers);

        Join(new JoinRequest { ClientId = 46523 }).Forget(); // simulate fake player joining, in a real server this would be triggered by a client connecting and sending a join request
        
        await CheckAllPlayersReady();
        //click ready

        Debug.Log("All players ready. Starting game...");

        SetState(GameState.Ready);

        await UniTask.Delay(1000);

        InitializePlayerDecks();

        SetState(GameState.Shuffling);
        
        await UniTask.Delay(1000); //shuffling

        await Game();      
    }

    private async UniTask Game()
    {
        while(state != GameState.GameOver)
        {
            Debug.Log("Starting round...");

            SetPlayersState(PlayerState.WaitingToDraw);

            SetState(GameState.Drawing);

            FakePlayerDraw().Forget(); // simulate fake player drawing, in a real server this would be triggered by a client sending a draw request

            Debug.Log("Waiting for players to draw...");

            await CheckAllPlayersDrawn();

            Debug.Log("All players drawn. Resolving round...");

            SetState(GameState.Resolving);

            await UniTask.Delay(500); // resolving

            ResolveResponse resolveResponse = await Resolve();

            Debug.Log("Round resolved. Sending response...");

            SendResolveResponse(resolveResponse);
        }
    }

    private async UniTaskVoid FakePlayerDraw()
    {
        await UniTask.Delay(UnityEngine.Random.Range(300, 800));

        DrawResponse drawResponse = await DrawCard(new DrawRequest { PlayerId = 0 });

        Debug.Log("Fake player drew card: " + drawResponse.PlayerCardId);
        
        client.OnEnemyDrawn(drawResponse);
    }

    private async UniTask<ResolveResponse> Resolve()
    {
        await UniTask.Delay(500); // simulate server latency

        Player player1 = players[0];
        Player player2 = players[1];

        int winner = Resolve(player1.Deck.DrawnCard, player2.Deck.DrawnCard);

        if(winner >= 0)
        {
            Player winnerPlayer = players[winner]; 

            winnerPlayer.WarDeck.AddCard(player1.Deck.DrawnCard);
            winnerPlayer.WarDeck.AddCard(player2.Deck.DrawnCard);

            return new ResolveResponse
            {
                WinnerId = players[winner].PlayerID,
                DeckInfos = GetDeckInfos(),
                isATie = false,
                
            };
        }
        else
        {
            return new ResolveResponse
            {
                isATie = true
            };
        }        
    }

    private Player GetPlayer(int playerId)
    {
        return players.FirstOrDefault(p => p.PlayerID == playerId);
    }

    private List<DeckInfo> GetDeckInfos()
    {
        return players.Select(p => new DeckInfo
        {
            PlayerId = p.PlayerID,
            DeckCardsLeft = p.Deck.CardsLeft,
            WarDeckCardsLeft = p.WarDeck.CardsLeft
        }).ToList();
    }

    public async UniTask<DrawResponse> DrawCard(DrawRequest drawRequest)
    {
        await UniTask.Delay(500); // simulate server latency

        Player player = GetPlayer(drawRequest.PlayerId);

        if(player.State == PlayerState.Drawn)
        {
            return new DrawResponse
            {
                Status = DrawResponseStatus.AlreadyDrawn
            };
        }

        CardData card = player.Deck.Draw();

        player.State = PlayerState.Drawn;

        Debug.Log("Player " + player.PlayerID + " drew card: " + card.Id);

        return new DrawResponse
        {
            PlayerId = drawRequest.PlayerId,
            PlayerCardId = card.Id,
            DeckInfo = new DeckInfo
            {
                DeckCardsLeft = player.Deck.CardsLeft,
                WarDeckCardsLeft = player.WarDeck.CardsLeft
             },
        };
    }

    private void SendResolveResponse(ResolveResponse response)
    {
        client.ReceiveResolveResponce(response);
    }

    private void SetPlayersState(PlayerState state)
    {
        players.ForEach(p => p.State = state);
    }

     private void SetState(GameState newState)
    {
        if (state == newState)
            return;

        state = newState;

        OnGameStateChanged?.Invoke(state);
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

