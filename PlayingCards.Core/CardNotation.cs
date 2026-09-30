namespace PlayingCards.Core;

/// <summary>
/// Разбор строковой нотации карты, например "A♠" или "10♥".
/// </summary>
public static class CardNotation
{
    /// <summary>
    /// Найти карту по нотации, изъять её из пула и вернуть.
    /// </summary>
    public static Card TakeCard(List<Card> pool, string notation)
    {
        var suit = notation[^1];
        var rank = notation[..^1];

        if (int.TryParse(rank, out var rankValue) == false)
        {
            rankValue = rank switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                _ => throw new Exception($"{rank} rank undefined"),
            };
        }

        var card = pool.FirstOrDefault(x => x.Rank.Value == rankValue && x.Suit.IconChar == suit);

        if (card == null)
        {
            throw new Exception($"{notation} not found");
        }

        pool.Remove(card);

        return card;
    }
}
