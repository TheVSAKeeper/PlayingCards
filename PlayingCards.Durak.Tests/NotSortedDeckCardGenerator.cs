namespace PlayingCards.Durak.Tests
{
    public class NotSortedDeckCardGenerator : RandomDeckCardGenerator
    {
        public override List<Card> GetCards()
        {
            return CardsHolder.GetCards();
        }
    }
}
