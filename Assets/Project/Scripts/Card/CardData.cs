using UnityEngine;

[CreateAssetMenu(fileName = "CardData", menuName = "Data/Cards/Card Data")]
public class CardData : ScriptableObject
{
    public string Id;
    public int Value;

    public Sprite FrontSprite;
}