public class Card
{
    public CardData Data { get; private set; }

    public Card(CardData data)
    {
        Data = data;
    }
}