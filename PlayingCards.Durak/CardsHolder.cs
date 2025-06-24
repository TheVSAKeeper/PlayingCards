namespace PlayingCards.Durak;

/// <summary>
/// Хранитель информации о картах.
/// </summary>
public static class CardsHolder
{
    private static List<Card>? _cards;

    public static IEnumerable<Card> Cards => _cards ??= GetCards();

    private static List<Card> GetCards()
    {
        List<CardRank> ranks =
        [
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
