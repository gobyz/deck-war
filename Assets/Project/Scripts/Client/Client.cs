using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using System.Threading;
public class Client : MonoBehaviour
{
    public static Client Instance;
    private Server server;
    private int clientId;
    private int playerId;
    private GameConfig gameConfig;
    
    [HideInInspector] public UnityEvent<GameState> OnGameStateReceived = new UnityEvent<GameState>();
    [HideInInspector] public UnityEvent<JoinResponse> OnJoinReceived = new UnityEvent<JoinResponse>();
    [HideInInspector] public UnityEvent<DrawResponse> OnDrawnReceived = new UnityEvent<DrawResponse>();   
    [HideInInspector] public UnityEvent<Resolve> OnResolveReceived = new UnityEvent<Resolve>();
    [HideInInspector] public UnityEvent<GameOver> OnGameOverReceived = new UnityEvent<GameOver>();
    [HideInInspector] public UnityEvent OnTimeoutStarted = new UnityEvent();
    [HideInInspector] public UnityEvent OnTimeoutEnded = new UnityEvent();

    private CancellationTokenSource timeoutCts = new();
    private bool timeoutStarted = false;

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

        UniTask<JoinResponse> joinTask = server.Join(joinRequest);

        timeoutCts = new();

        TimeoutCheck(timeoutCts.Token).Forget();

        JoinResponse joinResponse = await joinTask;

        timeoutCts.Cancel();

        if (timeoutStarted)
        {
            OnTimeoutEnded.Invoke(); 
        }

        if (joinResponse.Status == JoinResponseStatus.Success)
        {
           playerId = joinResponse.PlayerId;     
        }

        OnJoinReceived.Invoke(joinResponse);
    }  

    public async void Draw()
    {
        DrawRequest drawRequest = new DrawRequest { PlayerId = playerId };

        UniTask drawTask = server.RequestDraw(drawRequest);

        timeoutCts = new ();

        TimeoutCheck(timeoutCts.Token).Forget();    

        await drawTask;

        timeoutCts.Cancel();

        if (timeoutStarted)
        {
            OnTimeoutEnded.Invoke(); 
        }
    }

    public async UniTask TimeoutCheck(CancellationToken token)
    {
        await UniTask.Delay(gameConfig.ClientTimeoutDuration);

        token.ThrowIfCancellationRequested();

        OnTimeoutStarted.Invoke();

        timeoutStarted = true;      
    }

    public void ReceiveGameState(GameState state)
    {
        OnGameStateReceived.Invoke(state);
    }

    public void ReceiveDrawn(DrawResponse drawResponse)
    {
        OnDrawnReceived.Invoke(drawResponse);
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
