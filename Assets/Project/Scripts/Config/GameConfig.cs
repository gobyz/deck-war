using UnityEngine;

[CreateAssetMenu(menuName = "Data/Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Card Settings")]
    public CardTheme CardTheme;
}