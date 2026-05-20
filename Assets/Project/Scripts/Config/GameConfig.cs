using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Game Settings")]
    public int MaxPlayers = 2;
    [Header("Card Settings")]
    public CardTheme CardTheme;
    public float DrawAnimationDuration = 0.5f;
    public Ease DrawAnimationEase = Ease.Linear;   
    public Vector3 HideRotationAngle = new Vector3(0, 180, 0);
    public Ease HideRotationEase = Ease.Linear;
    public float HideRotationDuration = 0.5f;
    public float HideAnimationDuration = 0.5f;
    public Ease HideAnimationEase = Ease.Linear;
    public List<CardData> FullDeck = new List<CardData>();

    public CardData GetCardById(string id)
    {
        return FullDeck.Find(card => card.Id == id);
    }
}