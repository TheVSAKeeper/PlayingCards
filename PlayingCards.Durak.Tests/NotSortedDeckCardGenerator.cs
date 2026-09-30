namespace PlayingCards.Durak.Tests;

public class NotSortedDeckCardGenerator() : RandomDeckCardGenerator(CardsHolder.SmallDeck)
{
    public override List<Card> GetCards()
    {
        return CardsHolder.SmallDeck.ToList();
    }
}
