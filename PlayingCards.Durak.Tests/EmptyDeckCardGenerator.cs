namespace PlayingCards.Durak.Tests;

public class EmptyDeckCardGenerator : RandomDeckCardGenerator
{
    public override List<Card> GetCards()
    {
        return [];
    }
}