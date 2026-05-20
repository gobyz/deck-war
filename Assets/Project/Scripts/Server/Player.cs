using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int PlayerID;
    public Deck Deck { get; private set; }
    public Deck WarDeck { get; private set; }
    public PlayerState State;

    public Player(int playerId)
    {
        PlayerID = playerId;
        State = PlayerState.Waiting;
    }

    public void InitializeDecks()
    {
        Deck = new Deck(new List<CardData>());
        WarDeck = new Deck(new List<CardData>());
    }
}

public enum PlayerState { 
    Waiting, 
    Ready, 
    WaitingToDraw,
    Drawn, 
}
