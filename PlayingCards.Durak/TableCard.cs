namespace PlayingCards.Durak
{
    /// <summary>
    /// Игральная карта на столе.
    /// </summary>
    public class TableCard
    {
        /// <summary>
        /// Игральная карта на столе.
        /// </summary>
        /// <param name="game"><see cref="Game"/></param>
        /// <param name="attackCard"><see cref="AttackCard"/></param>
        public TableCard(Game game, Card attackCard)
        {
            AttackCard = attackCard;
            Game = game;
        }

        /// <summary>
        /// Карта, которой атаковали.
        /// </summary>
        public Card AttackCard { get; }

        /// <summary>
        /// Карта, которой защищались.
        /// </summary>
        public Card? DefenceCard { get; private set; }

        /// <summary>
        /// Игра.
        /// </summary>
        public Game Game { get; }

        /// <summary>
        /// Защититься.
        /// </summary>
        /// <param name="defenceCard">Карта для защиты.</param>
        public void Defence(Card defenceCard)
        {
            if (defenceCard.Suit == Game.Deck.TrumpCard.Suit)
            {
                if (AttackCard.Suit == Game.Deck.TrumpCard.Suit)
                {
                    if (defenceCard.Rank.Value > AttackCard.Rank.Value)
                    {
                        DefenceCard = defenceCard;
                    }
                    else
                    {
                        throw new Exception("defence card rank small");
                    }
                }
                else
                {
                    DefenceCard = defenceCard;
                }
            }
            else
            {
                if (defenceCard.Suit == Game.Deck.TrumpCard.Suit)
                {
                    if (defenceCard.Rank.Value > AttackCard.Rank.Value)
                    {
                        DefenceCard = defenceCard;
                    }
                    else
                    {
                        throw new Exception("defence card rank small");
                    }
                }
                else
                {
                    throw new Exception("defence suit invalid");
                }
            }
        }
    }
}
