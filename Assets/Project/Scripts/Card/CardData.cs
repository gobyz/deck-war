using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Data/Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string Id;
    public int Value;
    public Sprite FrontSprite;
    
    public static CardData GetCardDataById(string id)
    {   
        return GameConfigProvider.Instance.GameConfig.FullDeck.Find(card => card.Id == id);
    }
    
}