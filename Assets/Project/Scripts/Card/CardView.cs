using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Card card;
    private CardTheme theme;

    public void Initialize(Card card, CardTheme theme)
    {
        this.card = card;
        this.theme = theme;

        ShowFront();
    }

    public void ShowFront()
    {
        spriteRenderer.sprite = card.Data.FrontSprite;
    }

    public void ShowBack()
    {
        spriteRenderer.sprite = theme.BackSprite;
    }
}