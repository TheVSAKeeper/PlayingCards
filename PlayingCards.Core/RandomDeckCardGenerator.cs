namespace PlayingCards.Core;

/// <summary>
/// Генератор колоды.
/// </summary>
public class RandomDeckCardGenerator(IReadOnlyList<Card> sourceCards)
{
    /// <summary>
    /// Получить карты для колоды.
    /// </summary>
    /// <returns></returns>
    public virtual List<Card> GetCards()
    {
        return sourceCards
            .Select(card => new { Order = Random.Shared.Next(), Card = card })
            .OrderBy(x => x.Order)
            .Select(x => x.Card)
            .ToList();
    }
}
