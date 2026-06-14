namespace PlayingCards.Durak.Server;

/// <summary>
/// Тип хода, выбранного болванчиком.
/// </summary>
public enum BotMoveKind
{
    /// <summary>
    /// Ходить нечем — пропуск.
    /// </summary>
    None,

    /// <summary>
    /// Начать раунд атакой (стол пуст).
    /// </summary>
    StartAttack,

    /// <summary>
    /// Подкинуть карту в уже открытый раунд.
    /// </summary>
    Attack,

    /// <summary>
    /// Отбиться от неотбитой карты на столе.
    /// </summary>
    Defence,

    /// <summary>
    /// Взять карты (отбиться нечем).
    /// </summary>
    Take,
}

/// <summary>
/// Решение болванчика об одном ходе.
/// </summary>
/// <param name="Kind">Тип хода.</param>
/// <param name="CardIndexes">Индексы карт в руке (для атаки/подкида/защиты).</param>
/// <param name="AttackCardIndex">Индекс атакующей карты на столе (только для защиты).</param>
public readonly record struct BotMove(BotMoveKind Kind, int[] CardIndexes, int? AttackCardIndex = null)
{
    public static readonly BotMove Pass = new(BotMoveKind.None, []);
}

/// <summary>
/// Простой ИИ-болванчик для отладки. Принимает решения в терминах игрового движка:
/// карты адресуются индексом в руке (рука сама пересортирована движком).
/// </summary>
/// <remarks>
/// Логика намеренно примитивна: отбиваемся наименьшей валидной картой, ходим наименьшей
/// (предпочитая не-козырь), подкидываем одну карту совпадающего достоинства. Этого достаточно,
/// чтобы партия игралась сама и можно было отлаживать UI/сервер.
/// </remarks>
public static class BotBrain
{
    /// <summary>
    /// Выбрать один ход для болванчика. Не мутирует состояние — только решает.
    /// </summary>
    /// <param name="game">Игра.</param>
    /// <param name="bot">Игрок-болванчик.</param>
    /// <returns>Ход; <see cref="BotMove.Pass" />, если ходить нечем/не его очередь.</returns>
    public static BotMove DecideMove(Game game, Player bot)
    {
        if (game.Status != GameStatus.InProcess)
        {
            return BotMove.Pass;
        }

        var trumpSuit = game.Deck.TrumpCard?.Suit;

        if (trumpSuit == null)
        {
            return BotMove.Pass;
        }

        if (game.DefencePlayer == bot)
        {
            return DecideDefence(game, bot, trumpSuit);
        }

        if (game.ActivePlayer == bot && game.IsRoundStarted() == false)
        {
            return DecideStartAttack(bot, trumpSuit);
        }

        if (game.DefencePlayer != bot && game.IsRoundStarted())
        {
            return DecideThrowIn(game, bot);
        }

        return BotMove.Pass;
    }

    private static BotMove DecideDefence(Game game, Player bot, CardSuit trumpSuit)
    {
        var attackIndex = -1;

        for (var i = 0; i < game.Cards.Count; i++)
        {
            if (game.Cards[i].DefenceCard == null)
            {
                attackIndex = i;
                break;
            }
        }

        if (attackIndex == -1)
        {
            return BotMove.Pass;
        }

        var attackCard = game.Cards[attackIndex].AttackCard;
        var hand = bot.Hand.Cards;

        var bestIndex = -1;
        Card? bestCard = null;

        for (var i = 0; i < hand.Count; i++)
        {
            var candidate = hand[i];

            if (CanBeat(attackCard, candidate, trumpSuit) == false)
            {
                continue;
            }

            if (bestCard == null || IsCheaperDefence(candidate, bestCard, trumpSuit))
            {
                bestCard = candidate;
                bestIndex = i;
            }
        }

        if (bestIndex == -1)
        {
            return game.Cards.Count > 0 ? new BotMove(BotMoveKind.Take, []) : BotMove.Pass;
        }

        return new BotMove(BotMoveKind.Defence, [bestIndex], attackIndex);
    }

    private static BotMove DecideStartAttack(Player bot, CardSuit trumpSuit)
    {
        var hand = bot.Hand.Cards;

        if (hand.Count == 0)
        {
            return BotMove.Pass;
        }

        var bestIndex = -1;
        Card? bestCard = null;

        for (var i = 0; i < hand.Count; i++)
        {
            var candidate = hand[i];

            if (bestCard == null || IsCheaperAttack(candidate, bestCard, trumpSuit))
            {
                bestCard = candidate;
                bestIndex = i;
            }
        }

        return new BotMove(BotMoveKind.StartAttack, [bestIndex]);
    }

    private static BotMove DecideThrowIn(Game game, Player bot)
    {
        var undefended = game.Cards.Count(tableCard => tableCard.DefenceCard == null);
        var defenderHandCount = game.DefencePlayer?.Hand.Cards.Count ?? 0;

        if (game.Cards.Count >= 6 || undefended >= defenderHandCount)
        {
            return BotMove.Pass;
        }

        var ranksOnTable = new HashSet<int>();

        foreach (var tableCard in game.Cards)
        {
            ranksOnTable.Add(tableCard.AttackCard.Rank.Value);

            if (tableCard.DefenceCard != null)
            {
                ranksOnTable.Add(tableCard.DefenceCard.Rank.Value);
            }
        }

        var hand = bot.Hand.Cards;
        var bestIndex = -1;
        var bestRank = int.MaxValue;

        for (var i = 0; i < hand.Count; i++)
        {
            var candidate = hand[i];

            if (ranksOnTable.Contains(candidate.Rank.Value) == false)
            {
                continue;
            }

            if (candidate.Rank.Value < bestRank)
            {
                bestRank = candidate.Rank.Value;
                bestIndex = i;
            }
        }

        return bestIndex == -1
            ? BotMove.Pass
            : new BotMove(BotMoveKind.Attack, [bestIndex]);
    }

    /// <summary>
    /// Может ли <paramref name="defence" /> побить <paramref name="attack" /> по правилам.
    /// </summary>
    private static bool CanBeat(Card attack, Card defence, CardSuit trumpSuit)
    {
        var defenceIsTrump = defence.Suit == trumpSuit;
        var attackIsTrump = attack.Suit == trumpSuit;

        if (defenceIsTrump)
        {
            return attackIsTrump == false || defence.Rank.Value > attack.Rank.Value;
        }

        if (attackIsTrump)
        {
            return false;
        }

        return attack.Suit == defence.Suit && defence.Rank.Value > attack.Rank.Value;
    }

    /// <summary>
    /// «Дешевле» для защиты: не-козырь выгоднее козыря, среди равнозначных — меньший ранг.
    /// </summary>
    private static bool IsCheaperDefence(Card candidate, Card current, CardSuit trumpSuit)
    {
        var candidateTrump = candidate.Suit == trumpSuit;
        var currentTrump = current.Suit == trumpSuit;

        if (candidateTrump != currentTrump)
        {
            return candidateTrump == false;
        }

        return candidate.Rank.Value < current.Rank.Value;
    }

    /// <summary>
    /// «Дешевле» для атаки: не-козырь выгоднее козыря, среди равнозначных — меньший ранг.
    /// </summary>
    private static bool IsCheaperAttack(Card candidate, Card current, CardSuit trumpSuit)
    {
        return IsCheaperDefence(candidate, current, trumpSuit);
    }
}
