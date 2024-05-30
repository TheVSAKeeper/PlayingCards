
namespace PlayingCards.Durak
{
    /// <summary>
    /// Колода карт.
    /// </summary>
    public class Deck
    {
        private RandomDeckCardGenerator _cardGenerator;

        /// <summary>
        /// Колода карт.
        /// </summary>
        public Deck(RandomDeckCardGenerator cardGenerator)
        {
            Cards = new List<Card>();
            _cardGenerator = cardGenerator;
        }

        /// <summary>
        /// Козырь.
        /// </summary>
        public Card TrumpCard { get; set; }

        /// <summary>
        /// Количество карт в колоде.
        /// </summary>
        public int CardsCount => Cards.Count;

        /// <summary>
        /// Карты.
        /// </summary>
        public List<Card> Cards { get; set; }

        /// <summary>
        /// Перетусовать колоду.
        /// </summary>
        /// <remarks>
        /// Создать колоду с картами расположенными в случайном порядке.
        /// </remarks>
        public void Shuffle()
        {
            Cards = _cardGenerator.GetCards();
            TrumpCard = Cards.First();
        }

        /// <summary>
        /// Достать карту из колоды.
        /// </summary>
        /// <returns></returns>
        public Card PullCard()
        {
            var card = Cards.LastOrDefault();
            if (card == null)
            {
                throw new Exception("deck empty");
            }
            Cards.Remove(card);
            return card;
        }
    }
}
