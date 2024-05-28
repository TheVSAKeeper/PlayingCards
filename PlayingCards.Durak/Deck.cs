
namespace PlayingCards.Durak
{
    /// <summary>
    /// Колода карт.
    /// </summary>
    public class Deck
    {
        /// <summary>
        /// Колода карт.
        /// </summary>
        public Deck()
        {
            _cards = new List<Card>();
        }

        /// <summary>
        /// Карты.
        /// </summary>
        private List<Card> _cards { get; set; }

        /// <summary>
        /// Козырь.
        /// </summary>
        public Card TrumpCard { get; set; }

        /// <summary>
        /// Количество карт в колоде.
        /// </summary>
        public int CardsCount => _cards.Count;

        /// <summary>
        /// Перетусовать колоду.
        /// </summary>
        /// <remarks>
        /// Создать колоду с картами расположенными в случайном порядке.
        /// </remarks>
        public void Shuffle()
        {
            _cards = CardsHolder.GetCards()
                .Select(x => new { Order = Globals.Random.Next(), Card = x })
                .OrderBy(x => x.Order)
                .Select(x => x.Card).ToList();
            TrumpCard = _cards.First();
        }

        /// <summary>
        /// Достать карту из колоды.
        /// </summary>
        /// <returns></returns>
        public Card PullCard()
        {
            var card = _cards.LastOrDefault();
            if (card == null)
            {
                throw new Exception("deck empty");
            }
            _cards.Remove(card);
            return card;
        }
    }
}
