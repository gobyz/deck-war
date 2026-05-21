using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<CardData> cards = new();
    public int CardsLeft => cards.Count;

    public Deck(List<CardData> source)
    {
        cards = new List<CardData>(source);
    }

    public void Shuffle()
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            (cards[i], cards[r]) = (cards[r], cards[i]);
        }
    }
    public void AddCard(CardData cardData)
    {
        cards.Add(cardData);
    }

    public CardData Draw()
    {
        CardData card = cards[^1];
        cards.RemoveAt(cards.Count - 1);
        return card;
    }
}
