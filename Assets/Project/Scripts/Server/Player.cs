using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int PlayerID;
    public Deck Deck { get; private set; }
    public Deck WarDeck { get; private set; }
    public List<CardData> DrawnCards = new List<CardData>();
    public int DrawnCardsCount => DrawnCards.Count;
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
    public bool CanDraw()
    {
        return Deck.CardsLeft > 0 || WarDeck.CardsLeft > 0;
    }

    public CardData Draw()
    {
        if (CanDraw())
        {
            if(Deck.CardsLeft == 0)
            {
                WarDeck.Shuffle();
                Deck = WarDeck;
                WarDeck = new Deck(new List<CardData>());
            }

            return Deck.Draw();
        }
        else
        {
            Debug.LogError($"Player {PlayerID} cannot draw, no cards left in deck or war deck, this should not happen.");
            return null;
        } 
    }
}

public enum PlayerState { 
    Waiting, 
    Ready, 
}
