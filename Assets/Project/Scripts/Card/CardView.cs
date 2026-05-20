using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private CardData cardData;

    public void Initialize(string cardId)
    {
        CardData card = GameConfigProvider.Instance.GameConfig.GetCardById(cardId);
        this.cardData = card;

        ShowFront();
    }

    public void ShowFront()
    {
        spriteRenderer.sprite = cardData.FrontSprite;
    }


    public void ShowBack()
    {
        spriteRenderer.sprite = GameConfigProvider.Instance.GameConfig.CardTheme.BackSprite;
    }
}