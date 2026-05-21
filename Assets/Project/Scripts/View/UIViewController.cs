using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIViewController : MonoBehaviour
{
    [SerializeField] private Client client;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button drawButton;
    [SerializeField] private TMP_Text playerDeckCountText;
    [SerializeField] private TMP_Text playerWarDeckCountText;
    [SerializeField] private TMP_Text enemyDeckCountText;
    [SerializeField] private TMP_Text enemyWarDeckCountText;
    [SerializeField] private GameObject tieDeck;
    [SerializeField] private TMP_Text tieDeckCountText;
    
    private void Start()
    {
        FakeWarServer.OnGameStateSet += OnGameStateChanged;
        Client.OnPlayerCardDrawn.AddListener(OnPlayerCardDrawn);
        Client.OnEnemyCardDrawn.AddListener(OnEnemyCardDrawn);
        Client.OnResolveResponseReceived.AddListener(OnResolveResponseReceived);
        joinButton.onClick.AddListener(OnJoinClicked);
        drawButton.onClick.AddListener(OnDrawClicked);
    }
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
                ShowGameUI(false);
                SetJoinButton(true);
                break;

            case GameState.Ready:
                ShowGameUI(true);
                SetJoinButton(false);     
                break;

            case GameState.Shuffling:

                break;

            case GameState.Drawing:
                SetDrawButton(true);
                break;

            case GameState.Resolving:
                SetDrawButton(false);
                break;
            case GameState.GameOver:

                break;
        }
    }

    private void OnPlayerCardDrawn(string cardId, DeckData deckInfo)
    {
        playerDeckCountText.text = deckInfo.DeckCardsLeft.ToString();
        playerWarDeckCountText.text = deckInfo.WarDeckCardsLeft.ToString();
        drawButton.interactable = true;
    }

    private void OnEnemyCardDrawn(string cardId, DeckData deckInfo)
    {
        enemyDeckCountText.text = deckInfo.DeckCardsLeft.ToString();
        enemyWarDeckCountText.text = deckInfo.WarDeckCardsLeft.ToString();
    }

    private void OnResolveResponseReceived(ResolveResponse resolveResponse)
    {
        foreach(DeckData deckInfo in resolveResponse.DeckDatas)
        {
            if(Client.Instance.IsLocalPlayer(deckInfo.PlayerId))
            {
                playerDeckCountText.text = deckInfo.DeckCardsLeft.ToString();
                playerWarDeckCountText.text = deckInfo.WarDeckCardsLeft.ToString();
            }
            else
            {
                enemyDeckCountText.text = deckInfo.DeckCardsLeft.ToString();
                enemyWarDeckCountText.text = deckInfo.WarDeckCardsLeft.ToString();
            }
        }

        if(resolveResponse.IsATie)
        {
            tieDeck.SetActive(true);
            tieDeckCountText.text = resolveResponse.WonPileCardsCount.ToString();
        }
        else
        {
            tieDeck.SetActive(false);
        }
    }
    private void ShowGameUI(bool value)
    {
        gameUI.SetActive(value);
    }

    private void SetJoinButton(bool value)
    {
        joinButton.gameObject.SetActive(value);
        joinButton.interactable = value;
    }


    private void SetDrawButton(bool value)
    {
        drawButton.gameObject.SetActive(value);
        drawButton.interactable = value;
    }

    private void OnJoinClicked()
    {
        client.Join();

        joinButton.interactable = false;
    }

    private void OnDrawClicked()
    {
        client.DrawRequest();

        drawButton.interactable = false;
    }

    void OnDestroy()
    {
        joinButton.onClick.RemoveListener(OnJoinClicked);
        drawButton.onClick.RemoveListener(OnDrawClicked);
    }
}
