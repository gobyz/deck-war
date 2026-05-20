using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Game Settings")]
    public int MaxPlayers = 2;
    [Header("Card Settings")]
    public CardTheme CardTheme;
    public List<CardData> FullDeck = new List<CardData>();
}