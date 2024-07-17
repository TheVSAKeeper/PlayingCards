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
            if(DefenceCard != null)
            {
                throw new BusinessException("defence card exist");
            }
            if (defenceCard.Suit.Value == Game.Deck.TrumpCard.Suit.Value)
            {
                if (AttackCard.Suit.Value == Game.Deck.TrumpCard.Suit.Value)
                {
                    if (defenceCard.Rank.Value > AttackCard.Rank.Value)
                    {
                        DefenceCard = defenceCard;
                    }
                    else
                    {
                        throw new BusinessException("defence card rank small");
                    }
                }
                else
                {
                    DefenceCard = defenceCard;
                }
            }
            else
            {
                if (AttackCard.Suit.Value == Game.Deck.TrumpCard.Suit.Value)
                {
                    throw new BusinessException("defence suit is not trump");
                }
                else
                {
                    if(AttackCard.Suit.Value == defenceCard.Suit.Value)
                    {
                        if (defenceCard.Rank.Value > AttackCard.Rank.Value)
                        {
                            DefenceCard = defenceCard;
                        }
                        else
                        {
                            throw new BusinessException("defence card rank small");
                        }
                    }
                    else
                    {
                        throw new BusinessException("defence suit invalid");
                    }
                }
            }
        }
    }
}
