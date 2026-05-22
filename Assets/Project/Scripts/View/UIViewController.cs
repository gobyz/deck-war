using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIViewController : MonoBehaviour
{
    [Header("References")] 
    private GameConfig gameConfig;
    [SerializeField] private Client client;
    [SerializeField] private AudioController audioController;
    [Header("Shuffle UI")]
    [SerializeField] private GameObject shuffleUI;
    [SerializeField] private GameObject shuffleLabel;
    [Header("Game UI")]
    [SerializeField] private GameObject gameUI;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button drawButton;
    [SerializeField] private TMP_Text playerDeckCountText;
    [SerializeField] private TMP_Text playerWarDeckCountText;
    [SerializeField] private TMP_Text enemyDeckCountText;
    [SerializeField] private TMP_Text enemyWarDeckCountText;
    [SerializeField] private GameObject tieDeck;
    [SerializeField] private TMP_Text tieDeckCountText;
    [SerializeField] private List<Image> cardBackImages = new List<Image>();
    [Header("Win UI")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject winLabel;
    [SerializeField] private GameObject loseLabel;
    [SerializeField] private GameObject tieLabel;
    
    private void Start()
    {
        gameConfig = GameConfigProvider.Instance.GameConfig;

        client.OnGameStateReceived.AddListener(OnGameState);
        client.OnJoinReceived.AddListener(OnJoin);
        client.OnDrawnReceived.AddListener(OnDrawn);
        client.OnResolveReceived.AddListener(OnResolve);
        client.OnGameOverReceived.AddListener(OnGameOver);
        joinButton.onClick.AddListener(OnJoinClicked);
        drawButton.onClick.AddListener(OnDrawClicked);

        SetCardTheme();
    }

    private void SetCardTheme()
    {
        cardBackImages.ForEach(image => image.sprite = gameConfig.CardTheme.BackSprite);
    }

    private void OnGameState(GameState state)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
                ResetUI();
                break;

            case GameState.Shuffling:
                SetJoinButton(false);  
                ShowShuffleUI(true);
                break;

            case GameState.Ready:
                ShowShuffleUI(false);
                ShowGameUI(true);          
                break;

            case GameState.Drawing:
                SetDrawButton(true);
                break;

            case GameState.Resolving:
                SetDrawButton(false);
                break;
        }
    }

    private void ResetUI()
    {
        ShowGameUI(false);
        ShowWinUI(false);
        ShowShuffleUI(false);
        ShowTieDeck(false);
        SetJoinButton(true);

        playerDeckCountText.text = gameConfig.StartingDeckSize.ToString();
        enemyDeckCountText.text = gameConfig.StartingDeckSize.ToString();
        playerWarDeckCountText.text = "0"; 
        enemyWarDeckCountText.text = "0";
        tieDeckCountText.text = "0";
    }

    private void OnJoin(JoinResponse joinResponse)
    {
        if(joinResponse.Status == JoinResponseStatus.Error ||joinResponse.Status == JoinResponseStatus.ServerFull)
        {
            SetJoinButton(true);
        }
    }

    private void OnDrawn(DrawResponse drawResponse)
    {
        switch (drawResponse.Status)
        {
            case DrawResponseStatus.Error:
                SetDrawButton(true);
                break;

            case DrawResponseStatus.Success:
                if (client.IsLocalPlayer(drawResponse.PlayerId))
                {
                    playerDeckCountText.text = drawResponse.DeckInfo.DeckCardsLeft.ToString();
                    playerWarDeckCountText.text = drawResponse.DeckInfo.WarDeckCardsLeft.ToString();

                    drawButton.interactable = true;
                }
                else
                {
                    enemyDeckCountText.text = drawResponse.DeckInfo.DeckCardsLeft.ToString();
                    enemyWarDeckCountText.text = drawResponse.DeckInfo.WarDeckCardsLeft.ToString();
                }

                break;
        }
    }

    private void OnResolve(Resolve resolveResponse)
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

        if(resolveResponse.Outcome == Outcome.Tie)
        {
            tieDeck.SetActive(true);
            tieDeckCountText.text = resolveResponse.WonPileCardsCount.ToString();
        }
        else
        {
            tieDeck.SetActive(false);
        }
    }

    private void ShowTieDeck(bool value)
    {
        tieDeck.SetActive(value);
    }

    private void OnGameOver(GameOver gameOver)
    {
        ShowWinUI(true, gameOver.GameOutcome);
    }

    private void ShowShuffleUI(bool value)
    {
        shuffleUI.SetActive(value);
        shuffleLabel.SetActive(value);
        if (value)
        {
             shuffleLabel.transform.localScale = Vector3.zero;
             shuffleLabel.transform.DOScale(Vector3.one, gameConfig.ShuffleAnimationDuration);
             shuffleLabel.transform.DOShakePosition(gameConfig.ShuffleAnimationDuration, gameConfig.ShuffleAnimationStrength, gameConfig.ShuffleAnimationVibrato, gameConfig.ShuffleAnimationRandomness); 
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

        audioController.OnJoinClick();

        joinButton.interactable = false;
    }

    private void OnDrawClicked()
    {
        client.Draw();

        audioController.OnDrawClick();

        drawButton.interactable = false;
    }

    private void ShowWinUI(bool value, Outcome gameOutcome = Outcome.Undefined)
    {
        winLabel.SetActive(false);
        loseLabel.SetActive(false);
        tieLabel.SetActive(false);

        winUI.SetActive(value);

        switch (gameOutcome)
        {
            case Outcome.PlayerWin:
                winLabel.SetActive(true);
                break;
            case Outcome.EnemyWin:
                 loseLabel.SetActive(true);
                break;
            case Outcome.Tie:
                tieLabel.SetActive(true);
                break;
        }
    }

    void OnDestroy()
    {
        client.OnGameStateReceived.RemoveListener(OnGameState);
        client.OnDrawnReceived.RemoveListener(OnDrawn);
        client.OnResolveReceived.RemoveListener(OnResolve);
        joinButton.onClick.RemoveListener(OnJoinClicked);
        drawButton.onClick.RemoveListener(OnDrawClicked);
    }
}
