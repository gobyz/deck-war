using UnityEngine;

public class DrawResponse : MonoBehaviour
{
    public int PlayerId;
    public string PlayerCardId;
    public DeckData DeckInfo;
    public DrawResponseStatus Status;
}

public class DeckData
{
    public int PlayerId;
    public int DeckCardsLeft;
    public int WarDeckCardsLeft;
}

public enum DrawResponseStatus
{
    Error,
    Success,
    AlreadyDrawn,
    NoCardsLeft   
}