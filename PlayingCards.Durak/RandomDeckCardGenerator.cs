namespace PlayingCards.Durak;

/// <summary>
///     Генератор колоды.
/// </summary>
public class RandomDeckCardGenerator
{
    /// <summary>
    ///     Получить карты для колоды.
    /// </summary>
    /// <returns></returns>
    public virtual List<Card> GetCards()
    {
        return CardsHolder.Cards
            .Select(card => new { Order = Globals.Random.Next(), Card = card })
            .OrderBy(x => x.Order)
            .Select(x => x.Card)
            .ToList();
    }
}