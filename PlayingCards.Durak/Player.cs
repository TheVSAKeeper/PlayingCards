
namespace PlayingCards.Durak
{
    /// <summary>
    /// Игрок.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Игрок.
        /// </summary>
        public Player()
        {
            Hand = new PlayerHand();
        }
        /// <summary>
        /// Имя.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Рука.
        /// </summary>
        public PlayerHand Hand { get; set; }
    }

    /// <summary>
    /// Рука игрока с картами.
    /// </summary>
    public class PlayerHand
    {
        /// <summary>
        /// Рука игрока с картами.
        /// </summary>
        public PlayerHand()
        {
            _cards = new List<Card>();
        }

        /// <summary>
        /// Карты в руке.
        /// </summary>
        public IEnumerable<Card> Cards => _cards;

        private List<Card> _cards;

        /// <summary>
        /// Взять карту в руку.
        /// </summary>
        /// <param name="card">Карта.</param>
        public void TakeCard(Card card)
        {
            _cards.Add(card);
        }

        /// <summary>
        /// Очистить руку от карт.
        /// </summary>
        public void Clear()
        {
            _cards = new List<Card>();
        }

        /// <summary>
        /// Получить карту с минимальным значением козыря.
        /// </summary>
        /// <param name="suit">Масть козыря.</param>
        /// <returns>Карту, если она есть, иначе null.</returns>
        public Card? GetMinSuitCard(CardSuit suit)
        {
            return Cards
               .Where(x => x.Suit.Value == suit.Value)
               .OrderBy(x => x.Rank.Value)
               .FirstOrDefault();
        }
    }
}
