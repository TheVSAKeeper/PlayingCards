
namespace PlayingCards.Durak
{
    /// <summary>
    /// Рука игрока с картами.
    /// </summary>
    public class PlayerHand
    {
        /// <summary>
        /// Игра.
        /// </summary>
        private Game _game;

        /// <summary>
        /// Рука игрока с картами.
        /// </summary>
        public PlayerHand(Game game, Player player)
        {
            Cards = new List<Card>();
            _game = game;
            Player = player;
        }

        /// <summary>
        /// Игрок.
        /// </summary>
        public Player Player { get; }

        /// <summary>
        /// Карты в руке.
        /// </summary>
        public List<Card> Cards { get; set; }

        /// <summary>
        /// Взять карту в руку.
        /// </summary>
        /// <param name="card">Карта.</param>
        public void TakeCard(Card card)
        {
            Cards.Add(card);
        }

        /// <summary>
        /// Начать раунд, сыграв карту.
        /// </summary>
        /// <param name="cardIndex">Индекс карты.</param>
        public void StartAttack(int[] cardIndexes)
        {
            if(cardIndexes.Length == 0)
            {
                throw new BusinessException("need one or more card");
            }
            List<Card> cards = new List<Card>();
            foreach (var cardIndex in cardIndexes)
            {
                if (cardIndex < 0 || cardIndex >= Cards.Count)
                {
                    throw new BusinessException("undefined card");
                }
                var card = Cards[cardIndex];
                cards.Add(card);
            }
            _game.StartAttack(Player, cards);
        }

        /// <summary>
        /// Подкинуть карту.
        /// </summary>
        /// <param name="cardIndex">Индекс карты.</param>
        public void Attack(int[] cardIndexes)
        {
            List<Card> cards = new List<Card>();
            foreach (var cardIndex in cardIndexes)
            {
                if (cardIndex < 0 || cardIndex >= Cards.Count)
                {
                    throw new BusinessException("undefined card");
                }
                var card = Cards[cardIndex];
                cards.Add(card);
            }
            _game.Attack(Player, cards);
        }


        /// <summary>
        /// Защититься.
        /// </summary>
        /// <param name="defenceCardIndex">Индекс карты.</param>
        public void Defence(int defenceCardIndex, int attackCardIndex)
        {
            if (defenceCardIndex < 0 || defenceCardIndex >= Cards.Count)
            {
                throw new BusinessException("undefined card");
            }
            var card = Cards[defenceCardIndex];
            var attackTableCard = _game.Cards[attackCardIndex];
            _game.Defence(Player, card, attackTableCard.AttackCard);
        }


        /// <summary>
        /// Очистить руку от карт.
        /// </summary>
        public void Clear()
        {
            Cards = new List<Card>();
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

        public void Defence(object defenceCardIndex, object attackCardIndex)
        {
            throw new NotImplementedException();
        }

        public void Defence(int defenceCardIndex, object attackCardIndex)
        {
            throw new NotImplementedException();
        }
    }
}
