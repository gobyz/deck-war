using UnityEngine;
using UnityEngine.Events;

public class Client : MonoBehaviour
{
    public static Client Instance;
    public FakeWarServer server;
    public int clientId;
    private int playerId;
    public static UnityEvent<string, DeckInfo> OnPlayerCardDrawn = new UnityEvent<string, DeckInfo>();
    public static UnityEvent<string, DeckInfo> OnEnemyCardDrawn = new UnityEvent<string, DeckInfo>();
    public static UnityEvent OnPlayerWon = new UnityEvent();
    public static UnityEvent OnEnemyWon = new UnityEvent();
    public static UnityEvent OnTie = new UnityEvent();
    public static UnityEvent<ResolveResponse> OnResolveResponseReceived = new UnityEvent<ResolveResponse>();

    void Awake()
    {
        Instance = this;
    }

    private void Start()
    {        
        server = new FakeWarServer();
    }
    [ContextMenu("Join Game")]
    public async void Join()
    {
        JoinRequest joinRequest = new JoinRequest { ClientId = clientId };

        JoinResponse joinResponse = await server.Join(joinRequest, this);

        playerId = joinResponse.PlayerId;

        Debug.Log("Join response received. Status: " + joinResponse.Status);
    }
    [ContextMenu("Draw Card")]
    public async void DrawRequest()
    {
        DrawRequest drawRequest = new DrawRequest { PlayerId = playerId };
        
        DrawResponse drawResponse = await server.DrawCard(drawRequest);

        OnPlayerCardDrawn.Invoke(drawResponse.PlayerCardId, drawResponse.DeckInfo); 

        Debug.Log("Drew card: " + drawResponse.PlayerCardId);     
    }


    public void OnEnemyDrawn(DrawResponse drawResponse)
    {
        OnEnemyCardDrawn.Invoke(drawResponse.PlayerCardId, drawResponse.DeckInfo); 
    }


    public void ReceiveResolveResponce(ResolveResponse response)
    {
        OnResolveResponseReceived.Invoke(response);

        if (response.isATie)
        {
            OnTie.Invoke();
        }
        else
        {
            if(response.WinnerId == playerId)
            {
                OnPlayerWon.Invoke();
            }
            else
            {
                OnEnemyWon.Invoke();
            }
        }        
    }

    public bool IsLocalPlayer(int playerId)
    {
        return this.playerId == playerId;
    }
}
