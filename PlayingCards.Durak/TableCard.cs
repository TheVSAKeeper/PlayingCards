namespace PlayingCards.Durak;

/// <summary>
///     Игральная карта на столе.
/// </summary>
public class TableCard
{
    /// <summary>
    ///     Игральная карта на столе.
    /// </summary>
    /// <param name="game">
    ///     <see cref="Game" />
    /// </param>
    /// <param name="attackCard">
    ///     <see cref="AttackCard" />
    /// </param>
    public TableCard(Game game, Card attackCard)
    {
        AttackCard = attackCard;
        Game = game;
    }

    /// <summary>
    ///     Карта, которой атаковали.
    /// </summary>
    public Card AttackCard { get; }

    /// <summary>
    ///     Карта, которой защищались.
    /// </summary>
    public Card? DefenceCard { get; private set; }

    /// <summary>
    ///     Игра.
    /// </summary>
    private Game Game { get; }

    /// <summary>
    ///     Защититься.
    /// </summary>
    /// <param name="defenceCard">Карта для защиты.</param>
    public void Defence(Card defenceCard)
    {
        if (DefenceCard != null)
        {
            throw new BusinessException("Карта для защиты уже существует");
        }

        if (defenceCard.Suit == Game.Deck.TrumpCard.Suit)
        {
            if (AttackCard.Suit == Game.Deck.TrumpCard.Suit)
            {
                if (defenceCard.Rank > AttackCard.Rank)
                {
                    DefenceCard = defenceCard;
                }
                else
                {
                    throw new BusinessException("Ранг карты для защиты слишком низкий");
                }
            }
            else
            {
                DefenceCard = defenceCard;
            }
        }
        else
        {
            if (AttackCard.Suit == Game.Deck.TrumpCard.Suit)
            {
                throw new BusinessException("Масть карты для защиты не является козырной");
            }

            if (AttackCard.Suit == defenceCard.Suit)
            {
                if (defenceCard.Rank > AttackCard.Rank)
                {
                    DefenceCard = defenceCard;
                }
                else
                {
                    throw new BusinessException("Ранг карты для защиты слишком низкий");
                }
            }
            else
            {
                throw new BusinessException("Масть карты для защиты недействительна");
            }
        }
    }

    public override string ToString()
    {
        return AttackCard + (DefenceCard == null ? string.Empty : $"->{DefenceCard}");
    }
}