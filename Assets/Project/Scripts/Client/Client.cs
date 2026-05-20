using Cysharp.Threading.Tasks;
using UnityEngine;

public class Client : MonoBehaviour
{
    public FakeWarServer server;
    public int clientId;
    private int playerId;

    private void Start()
    {
        server = new FakeWarServer();
    }
    [ContextMenu("Join Game")]
    public async void Join()
    {
        JoinRequest joinRequest = new JoinRequest { ClientId = clientId };

        JoinResponse joinResponse = await server.Join(joinRequest);

        Debug.Log("Join response received. Status: " + joinResponse.Status);
    }
    [ContextMenu("Draw Card")]
    public async void DrawRequest()
    {
        DrawRequest drawRequest = new DrawRequest { PlayerId = playerId };
        
        DrawResponse drawResponse = await server.DrawCard(drawRequest);

        Debug.Log("Drew card: " + drawResponse.PlayerCardId);  
    }

    public void OnResponseReceived(ResolveResponse response)
    {
        if (response.isATie)
        {
            Debug.Log("It's a tie!");
        }
        else
        {
            if(response.PlayerId == playerId)
            {
                Debug.Log("You win!");
            }
            else
            {
                Debug.Log("You lose!");
            }
        }        
    }
}
