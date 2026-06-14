using PlayingCards.Durak;
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

    /// <summary>
    /// Возвращает множество индексов карт в руке игрока, которые можно сыграть в текущей ситуации.
    /// <paramref name="selectedHand"/> — уже выбранные индексы руки (для фильтра ранга стартовой атаки).
    /// <paramref name="selectedField"/> — уже выбранные индексы атакующих карт на поле (для защиты).
    /// Если рука пуста или ход не наш — возвращает пустое множество.
    /// </summary>
    public static HashSet<int> GetPlayableHandIndexes(TableModel table, HashSet<int> selectedHand, HashSet<int> selectedField)
    {
        var result = new HashSet<int>();

        if (table.MyCards == null || table.MyCards.Length == 0)
        {
            return result;
        }

        var tableCards = table.Cards ?? [];
        var tableCardsCount = tableCards.Length;
        var isMyTurn = table.ActivePlayerIndex == table.MyPlayerIndex;
        var isMyDefence = table.DefencePlayerIndex == table.MyPlayerIndex;

        if (isMyTurn && tableCardsCount == 0)
        {
            if (selectedHand.Count > 0)
            {
                var firstIdx = selectedHand.Min();
                var allowedRank = firstIdx < table.MyCards.Length ? table.MyCards[firstIdx].Rank : -1;

                for (var i = 0; i < table.MyCards.Length; i++)
                {
                    if (table.MyCards[i].Rank == allowedRank)
                    {
                        result.Add(i);
                    }
                }
            }
            else
            {
                for (var i = 0; i < table.MyCards.Length; i++)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        if (!isMyDefence && tableCardsCount > 0)
        {
            var ranksOnTable = new HashSet<int>();

            foreach (var tc in tableCards)
            {
                if (tc.AttackCard != null) ranksOnTable.Add(tc.AttackCard.Rank);
                if (tc.DefenceCard != null) ranksOnTable.Add(tc.DefenceCard.Rank);
            }

            for (var i = 0; i < table.MyCards.Length; i++)
            {
                if (ranksOnTable.Contains(table.MyCards[i].Rank))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        if (isMyDefence && table.Trump != null)
        {
            if (selectedField.Count == 1)
            {
                var fieldIdx = selectedField.First();

                if (fieldIdx < tableCards.Length && tableCards[fieldIdx].DefenceCard == null)
                {
                    var attackCard = tableCards[fieldIdx].AttackCard!;

                    for (var i = 0; i < table.MyCards.Length; i++)
                    {
                        if (IsValidDefence(attackCard, table.MyCards[i], table.Trump))
                        {
                            result.Add(i);
                        }
                    }

                    return result;
                }
            }

            var unbeatenAttacks = tableCards
                .Where(tc => tc.DefenceCard == null && tc.AttackCard != null)
                .Select(tc => tc.AttackCard!)
                .ToList();

            for (var i = 0; i < table.MyCards.Length; i++)
            {
                foreach (var attack in unbeatenAttacks)
                {
                    if (IsValidDefence(attack, table.MyCards[i], table.Trump))
                    {
                        result.Add(i);
                        break;
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Возвращает множество индексов атакующих карт на столе, которые игрок-защитник может побить.
    /// Отбитые карты (DefenceCard != null) не включаются.
    /// </summary>
    public static HashSet<int> GetBeatableFieldIndexes(TableModel table, HashSet<int> selectedHand)
    {
        var result = new HashSet<int>();

        if (table.DefencePlayerIndex != table.MyPlayerIndex
            || table.Trump == null
            || table.Cards == null
            || table.MyCards == null)
        {
            return result;
        }

        if (selectedHand.Count == 1)
        {
            var handIdx = selectedHand.First();

            if (handIdx < table.MyCards.Length)
            {
                var defenceCard = table.MyCards[handIdx];

                for (var i = 0; i < table.Cards.Length; i++)
                {
                    var tc = table.Cards[i];

                    if (tc.DefenceCard == null && tc.AttackCard != null
                        && IsValidDefence(tc.AttackCard, defenceCard, table.Trump))
                    {
                        result.Add(i);
                    }
                }

                return result;
            }
        }

        for (var i = 0; i < table.Cards.Length; i++)
        {
            var tc = table.Cards[i];

            if (tc.DefenceCard != null || tc.AttackCard == null)
            {
                continue;
            }

            foreach (var myCard in table.MyCards)
            {
                if (IsValidDefence(tc.AttackCard, myCard, table.Trump))
                {
                    result.Add(i);
                    break;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Контекстная подсказка игроку — что сейчас делать.
    /// </summary>
    public static string? GetContextHint(TableModel table)
    {
        if (table.Status != (int)GameStatus.InProcess)
        {
            return null;
        }

        var tableCardsCount = table.Cards?.Length ?? 0;
        var isMyTurn = table.ActivePlayerIndex == table.MyPlayerIndex;
        var isMyDefence = table.DefencePlayerIndex == table.MyPlayerIndex;

        if (isMyTurn && tableCardsCount == 0)
        {
            return "Ваш ход — выберите карту для атаки";
        }

        if (isMyDefence)
        {
            return "Выберите атакующую карту и карту для отбоя";
        }

        if (!isMyTurn && !isMyDefence && tableCardsCount > 0)
        {
            return "Можно подкинуть карту того же ранга";
        }

        return null;
    }
}
