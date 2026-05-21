using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class Client : MonoBehaviour
{
    public static Client Instance;
    private Server server;
    private int clientId;
    private int playerId;
    private GameConfig gameConfig;
    [HideInInspector] public UnityEvent<GameState> OnGameStateReceived = new UnityEvent<GameState>();
    [HideInInspector] public UnityEvent<DrawResponse> OnDrawnReceived = new UnityEvent<DrawResponse>();   
    [HideInInspector] public UnityEvent<Resolve> OnResolveReceived = new UnityEvent<Resolve>();
    [HideInInspector] public UnityEvent<GameOver> OnGameOverReceived = new UnityEvent<GameOver>();

    void Awake()
    {
        Instance = this;       
    }

    private void Start()
    {   
        gameConfig = GameConfigProvider.Instance.GameConfig;
        clientId = gameConfig.ClientIdPlayer;    
        server = new Server(this);
    }

    public bool IsLocalPlayer(int playerId)
    {
        return this.playerId == playerId;
    }

    public async void Join()
    {
        JoinRequest joinRequest = new JoinRequest { ClientId = clientId };

        JoinResponse joinResponse = await server.Join(joinRequest);

        if(joinResponse.Status == JoinResponseStatus.Success)
        {
            playerId = joinResponse.PlayerId;    
        }
    }

    public async void Draw()
    {
        DrawRequest drawRequest = new DrawRequest { PlayerId = playerId };
        
        server.RequestDraw(drawRequest).Forget();
    }

    public void ReceiveGameState(GameState state)
    {
        OnGameStateReceived.Invoke(state);
    }

    public void ReceiveDrawn(DrawResponse drawResponse)
    {
        if(drawResponse.Status == DrawResponseStatus.Success)
        {
            OnDrawnReceived.Invoke(drawResponse);
        }  
    }

    public void ReceiveResolve(Resolve resolve)
    {
        OnResolveReceived.Invoke(resolve);      
    }

    public void ReceiveGameOver(GameOver gameOver)
    {       
        OnGameOverReceived.Invoke(gameOver);
    }    
}
