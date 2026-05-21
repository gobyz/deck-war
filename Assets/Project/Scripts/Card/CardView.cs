using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    private CardData cardData;

    public void Initialize(string cardId, bool faceDown = true)
    {
        cardData = CardData.GetCardDataById(cardId);

        if (faceDown)
        {
            ShowBack();   
        }
        else
        {
            ShowFront();
        }       
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