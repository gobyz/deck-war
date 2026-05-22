using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    public readonly int MaxPlayers = 2;
    public readonly int StartingDeckSize = 26;
    public readonly int WarCardsCount = 4;
    [Header("Emulation")]
    [Range(0f,100f)]
    public float JoinErrorChance; 
    [Range(0f,100f)]
    public float ServerFullChance;
    [Range(0f,100f)]
    public float DrawErrorChance;
    [Range(0f,100f)]
    public float JoinTimeoutChance; 
    [Range(0f,100f)]
    public float DrawTimeoutChance;  
    public int ServerTimeoutDuration = 10000;
    public int ClientTimeoutDuration = 5000;  
    [Header("Clients")]
    public int ClientIdPlayer = 35221;
    public int ClientIdEnemy = 46523;
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
    [Header("Shuffle")]
    public float ShuffleAnimationDuration = 0.5f;
    public float ShuffleAnimationStrength = 0.5f;
    public int ShuffleAnimationVibrato = 10;
    public float ShuffleAnimationRandomness = 0.5f;
}