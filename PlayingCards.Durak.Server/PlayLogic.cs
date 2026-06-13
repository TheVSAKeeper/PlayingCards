using static PlayingCards.Durak.Server.GetStatusModel;

namespace PlayingCards.Durak.Server;

/// <summary>
/// Клиентская логика гейтинга хода: валидность защиты и возможность сыграть. Порт из game.js.
/// </summary>
public static class PlayLogic
{
    public static bool IsValidDefence(CardModel attackCard, CardModel defenceCard, CardModel trump)
    {
        if (defenceCard.Suit == trump.Suit)
        {
            if (attackCard.Suit == trump.Suit)
            {
                return defenceCard.Rank > attackCard.Rank;
            }

            return true;
        }

        if (attackCard.Suit == trump.Suit)
        {
            return false;
        }

        if (attackCard.Suit == defenceCard.Suit)
        {
            return defenceCard.Rank > attackCard.Rank;
        }

        return false;
    }

    /// <summary>
    /// Можно ли сыграть выбранные карты (атака/подкид/защита). Порт canPlayCards.
    /// </summary>
    public static bool CanPlayCards(TableModel table, int[] handIndexes, int[] fieldIndexes)
    {
        var tableCardsCount = table.Cards?.Length ?? 0;

        var isStartAttacking = handIndexes.Length > 0
            && table.ActivePlayerIndex == table.MyPlayerIndex
            && tableCardsCount == 0;

        var isAttacking = handIndexes.Length > 0
            && table.DefencePlayerIndex != table.MyPlayerIndex
            && tableCardsCount > 0;

        var isDefending = handIndexes.Length == 1
            && fieldIndexes.Length == 1
            && table.DefencePlayerIndex == table.MyPlayerIndex
            && table.Trump != null
            && IsValidDefence(table.Cards![fieldIndexes[0]].AttackCard!, table.MyCards![handIndexes[0]], table.Trump);

        return isStartAttacking || isAttacking || isDefending;
    }
}
