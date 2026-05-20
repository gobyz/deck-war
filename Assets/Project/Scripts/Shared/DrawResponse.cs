using UnityEngine;

public class DrawResponse : MonoBehaviour
{
    public int PlayerId;
    public string PlayerCardId;
    public DrawResponseStatus Status;
    public DeckInfo DeckInfo;
}

public class DeckInfo
{
    public int PlayerId;
    public int DeckCardsLeft;
    public int WarDeckCardsLeft;
}

public enum DrawResponseStatus
{
    Success,
    AlreadyDrawn,
    Error
}