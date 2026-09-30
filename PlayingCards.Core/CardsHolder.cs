namespace PlayingCards.Core;

/// <summary>
/// Хранитель информации о картах.
/// </summary>
public static class CardsHolder
{
    private static readonly Lazy<IReadOnlyList<Card>> LazyFullDeck = new(BuildFullDeck);

    private static readonly Lazy<IReadOnlyList<Card>> LazySmallDeck =
        new(() => LazyFullDeck.Value.Where(card => card.Rank.Value >= 6).ToList());

    public static IReadOnlyList<Card> FullDeck => LazyFullDeck.Value;

    public static IReadOnlyList<Card> SmallDeck => LazySmallDeck.Value;

    private static IReadOnlyList<Card> BuildFullDeck()
    {
        List<CardRank> ranks =
        [
            new(2, "2"),
            new(3, "3"),
            new(4, "4"),
            new(5, "5"),
            new(6, "6"),
            new(7, "7"),
            new(8, "8"),
            new(9, "9"),
            new(10, "10"),
            new(11, "Jack"),
            new(12, "Queen"),
            new(13, "King"),
            new(14, "Ace"),
        ];

        List<CardSuit> suits =
        [
            new(0, "Clubs", '♣'),
            new(1, "Diamonds", '♦'),
            new(2, "Hearts", '♥'),
            new(3, "Spades", '♠'),
        ];

        return suits.SelectMany(_ => ranks, (suit, rank) => new Card(rank, suit)).ToList();
    }
}
