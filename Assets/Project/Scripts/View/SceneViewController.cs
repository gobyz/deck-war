using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SceneViewController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Client client;
    private GameConfig gameConfig;
    [Header("Card")]
    [SerializeField] private CardView cardViewPrefab;
    [Header("Anchors")]
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

    private void Start()
    {
        gameConfig = GameConfigProvider.Instance.GameConfig;

        client.OnGameStateReceived.AddListener(OnGameState);
        client.OnDrawnReceived.AddListener(OnDrawn);
        client.OnResolveReceived.AddListener(OnResolveReceived);
    }

    private void OnGameState(GameState state)
    {
        if(state == GameState.WaitingForPlayers)
        {
            RemoveActiveCards();
        }
        if(state == GameState.Drawing)
        {
            ResetCardCounts();
        }
    }
    
    private void ResetCardCounts()
    {
        playerCardCount = 0;
        enemyCardCount = 0;
    }

    private void OnDrawn(DrawResponse drawResponse)
    {
        if (Client.Instance.IsLocalPlayer(drawResponse.PlayerId))
        {
            DrawCard(drawResponse, false);
        }
        else
        {
            DrawCard(drawResponse, true);
        }
    }

    private void DrawCard(DrawResponse drawResponse, bool isEnemy)
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
            cardView.Initialize(drawResponse.CardId, drawResponse.IsWar && enemyCardCount != gameConfig.WarCardsCount);
        }
        else
        {
            startAnchor = playerCardAnchors[playerCardCount].StartDrawAnchor;
            endAnchor = playerCardAnchors[playerCardCount].EndDrawAnchor;

            playerCardCount++;
            cardView.Initialize(drawResponse.CardId, drawResponse.IsWar && playerCardCount != gameConfig.WarCardsCount);
        }

        cardView.transform.position = startAnchor.position;
        cardView.transform.rotation = startAnchor.rotation;

        cardView.transform.DOLocalMove(endAnchor.localPosition, gameConfig.DrawAnimationDuration).SetEase(gameConfig.DrawAnimationEase);
    }

       private void OnResolveReceived(Resolve resolve)
    {
        Transform targetAnchor;

        if (resolve.IsATie)
        {
            targetAnchor = tieDeckAnchor;
        }
        else
        {
            if (Client.Instance.IsLocalPlayer(resolve.WinnerId))
            {
                targetAnchor = playerDrawStartAnchor;
            }
            else
            {
                targetAnchor = enemyDrawStartAnchor;
            }
        }

        foreach(CardView cardView in activeCardViews)
        {
            cardView.transform.DOLocalRotate(gameConfig.HideRotationAngle, gameConfig.HideRotationDuration, RotateMode.LocalAxisAdd).SetEase(gameConfig.HideRotationEase);
            cardView.transform.DOLocalMove(targetAnchor.localPosition, gameConfig.HideAnimationDuration).SetEase(gameConfig.HideAnimationEase).onComplete = () => RemoveActiveCards();
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

    private void OnDestroy()
    {
        client.OnGameStateReceived.RemoveListener(OnGameState);
        client.OnDrawnReceived.RemoveListener(OnDrawn);
        client.OnResolveReceived.RemoveListener(OnResolveReceived);   
    }
}
