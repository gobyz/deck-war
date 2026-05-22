using System.Collections.Generic;
using UnityEngine;

public class PopupController : MonoBehaviour
{
    [SerializeField] private Client client;
    [SerializeField] private List<Popup> popups = new List<Popup>();

    private void Start()
    {
        HideAll();

        client.OnJoinReceived.AddListener(OnJoin);
        client.OnDrawnReceived.AddListener(OnDraw);
        client.OnTimeoutStarted.AddListener(OnTimeoutStarted);
        client.OnTimeoutEnded.AddListener(OnTimeoutEnded);
    }

    private void HideAll()
    {
        popups.ForEach(p => p.Hide());
    }

    private void Show(PopupType type)
    {
        if (type == PopupType.Undefined)
        {
            Debug.LogError("Popup type is undefined. This should not happen.");

            return;
        }

        HideAll();

        popups.Find(p => p.PopupType == type).Show();
    }

    private void Hide(PopupType type)
    {
        popups.Find(p => p.PopupType == type).Hide();
    }

    private void OnJoin(JoinResponse joinResponse)
    {
        switch (joinResponse.Status)
        {   
            case JoinResponseStatus.Error:
                Show(PopupType.Error);
                break;

            case JoinResponseStatus.ServerFull:
                Show(PopupType.ServerFull);
                break;
        }
    }

    private void OnDraw(DrawResponse drawnResponse)
    {
        if (drawnResponse.Status == DrawResponseStatus.Error)
        {
            Show(PopupType.Error);
        }
    }

    private void OnTimeoutStarted()
    {
        Show(PopupType.Timeout);
    }

    private void OnTimeoutEnded()
    {
        Hide(PopupType.Timeout);
    }

    private void OnDestroy()
    {
        client.OnJoinReceived.RemoveListener(OnJoin);
    }
}
