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
    private enum GameState
    {
        WaitingForPlayers,
        Ready,
        Shuffling,
        Drawing,
        Resolving,
        GameOver
    }

    private GameState state;

    private List<Player> players = new List<Player>();

    private Client client; // only one client because this is a fake server, in a real server this would be a list of clients

    private CancellationTokenSource cts = new();

    public async UniTask<JoinResponse> Join(JoinRequest joinRequest, Client client = null)
    {
        await UniTask.Delay(500); // simulate server latency

        if(players.Count < gameConfig.MaxPlayers)
        {
            Debug.Log("Player " + joinRequest.ClientId + " joined the game.");

            Player player = new Player(joinRequest.ClientId);

            players.Add(player);

            player.State = PlayerState.Ready;

            player.PlayerID = players.IndexOf(player);

            this.client = client;

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
  
        state = GameState.WaitingForPlayers;

        Join(new JoinRequest { ClientId = 46523 }).Forget(); // simulate fake player joining, in a real server this would be triggered by a client connecting and sending a join request
        
        await CheckAllPlayersReady();
        //click ready

        state = GameState.Ready;

        await UniTask.Delay(1000);

        InitializePlayerDecks();

        state = GameState.Shuffling;
        
        await UniTask.Delay(1000); //shuffling

        await Game();      
    }

    private async UniTask Game()
    {
        while(state != GameState.GameOver)
        {
            state = GameState.Drawing;

            await CheckAllPlayersDrawn();

            await UniTask.Delay(500); // resolving

            ResolveResponse resolveResponse = await Resolve();

            SendResolveResponse(resolveResponse);
        }
    }

   

    private async UniTask<ResolveResponse> Resolve()
    {
        await UniTask.Delay(500); // simulate server latency

        Player player1 = players[0];
        Player player2 = players[1];

        int winner = Resolve(player1.Deck.drawnCard, player2.Deck.drawnCard);

        if(winner > 0)
        {
            Player winnerPlayer = players[winner]; 

            winnerPlayer.WarDeck.AddCard(player1.Deck.drawnCard);
            winnerPlayer.WarDeck.AddCard(player2.Deck.drawnCard);

            return new ResolveResponse
            {
                PlayerId = players[winner].PlayerID,
                isATie = false
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

    public async UniTask<DrawResponse> DrawCard(DrawRequest drawRequest)
    {
        await UniTask.Delay(500); // simulate server latency

        Player player = GetPlayer(drawRequest.PlayerId);

        CardData card = player.Deck.Draw();

        player.State = PlayerState.Drawn;

        return new DrawResponse
        {
            PlayerId = drawRequest.PlayerId,
            PlayerCardId = card.Id
        };
    }

    private void SendResolveResponse(ResolveResponse response)
    {
        client.OnResponseReceived(response);
    }
}

