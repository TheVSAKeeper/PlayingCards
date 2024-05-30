namespace PlayingCards.Durak
{
    /// <summary>
    /// Геренаратор колоды.
    /// </summary>
    public class RandomDeckCardGenerator
    {
        /// <summary>
        /// Получить карты для колоды.
        /// </summary>
        /// <returns></returns>
        public virtual List<Card> GetCards()
        {
            return CardsHolder.GetCards()
                .Select(x => new { Order = Globals.Random.Next(), Card = x })
                .OrderBy(x => x.Order)
                .Select(x => x.Card).ToList();
        }
    }
}
