using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SceneViewController : MonoBehaviour
{
    private GameConfig gameConfig;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private Transform playerDrawStartAnchor;
    [SerializeField] private Transform playerDrawEndAnchor;
    [SerializeField] private Transform enemyDrawStartAnchor;
    [SerializeField] private Transform enemyDrawEndAnchor;
    [SerializeField] private Transform tieDeckAnchor;
    [SerializeField] private List<Anchor> playerCardAnchors = new List<Anchor>();
    [SerializeField] private List<Anchor> enemyCardAnchors = new List<Anchor>();
    private int playerCardCount;
    private int enemyCardCount;
    private List<CardView> activeCardViews = new List<CardView>();


    private void Awake()
    {
        gameConfig = GameConfigProvider.Instance.GameConfig;
        FakeWarServer.OnGameStateSet += OnGameStateChanged;
        Client.OnPlayerCardDrawn.AddListener(PlayerDraw);
        Client.OnEnemyCardDrawn.AddListener(EnemyDraw);
        Client.OnPlayerWon.AddListener(OnPlayerWon);
        Client.OnEnemyWon.AddListener(OnEnemyWon);
        Client.OnTie.AddListener(OnTie);
    }

    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.WaitingForPlayers:
                
                break;

            case GameState.Ready:
                
                break;

            case GameState.Shuffling:

                break;

            case GameState.Drawing:
                playerCardCount = 0;
                enemyCardCount = 0;
                break;

            case GameState.Resolving:
                
                break;
            case GameState.GameOver:

                break;
        }
    }

    private void PlayerDraw(string cardId, DeckData deckInfo)
    { 
        DrawCard(cardId, false);
    }

    private void EnemyDraw(string cardId, DeckData deckInfo)
    {
        DrawCard(cardId, true);
    }

    private void DrawCard(string cardId, bool isEnemy)
    {
        CardView cardView = Instantiate(cardViewPrefab);
        activeCardViews.Add(cardView);

        Transform startAnchor;
        Transform endAnchor;

        if (isEnemy)
        {
            startAnchor = enemyCardAnchors[enemyCardCount].StartDrawAnchor;
            endAnchor = enemyCardAnchors[enemyCardCount].EndDrawAnchor;
            enemyCardCount++;
        }
        else
        {
            startAnchor = playerCardAnchors[playerCardCount].StartDrawAnchor;
            endAnchor = playerCardAnchors[playerCardCount].EndDrawAnchor;
            playerCardCount++;
        }

        cardView.transform.position = startAnchor.position;
        cardView.transform.rotation = startAnchor.rotation; 

        cardView.Initialize(cardId);

        cardView.transform.DOLocalMove(endAnchor.localPosition, gameConfig.DrawAnimationDuration).SetEase(gameConfig.DrawAnimationEase);
    }

    private void OnPlayerWon()
    {
        foreach(CardView cardView in activeCardViews)
        {
            cardView.transform.DOLocalRotate(gameConfig.HideRotationAngle, gameConfig.HideRotationDuration, RotateMode.LocalAxisAdd).SetEase(gameConfig.HideRotationEase);
            cardView.transform.DOLocalMove(playerDrawStartAnchor.localPosition, gameConfig.HideAnimationDuration).SetEase(gameConfig.HideAnimationEase).onComplete = () => RemoveActiveCards();
        }
    }

    private void OnEnemyWon()
    {
        foreach(CardView cardView in activeCardViews)
        {
            cardView.transform.DOLocalRotate(gameConfig.HideRotationAngle, gameConfig.HideRotationDuration, RotateMode.LocalAxisAdd).SetEase(gameConfig.HideRotationEase);
            cardView.transform.DOLocalMove(enemyDrawStartAnchor.localPosition, gameConfig.HideAnimationDuration).SetEase(gameConfig.HideAnimationEase).onComplete = () => RemoveActiveCards();
        }
    }


    private void OnTie()
    {
        foreach(CardView cardView in activeCardViews)
        {
            cardView.transform.DOLocalRotate(gameConfig.HideRotationAngle, gameConfig.HideRotationDuration, RotateMode.LocalAxisAdd).SetEase(gameConfig.HideRotationEase);
            cardView.transform.DOLocalMove(tieDeckAnchor.localPosition, gameConfig.HideAnimationDuration).SetEase(gameConfig.HideAnimationEase).onComplete = () => RemoveActiveCards();
        }
    }

    private void RemoveActiveCards()
    {
        foreach (var cardView in activeCardViews)
        {
            Destroy(cardView.gameObject);
        }
        activeCardViews.Clear();
    }

    private void OnDisable()
    {
        Client.OnPlayerCardDrawn.RemoveListener(PlayerDraw);
        Client.OnEnemyCardDrawn.RemoveListener(EnemyDraw);
    }
}
