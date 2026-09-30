namespace PlayingCards.Durak.Tests;

public class EmptyDeckCardGenerator() : RandomDeckCardGenerator(CardsHolder.SmallDeck)
{
    public override List<Card> GetCards()
    {
        return [];
    }
}
