namespace PlayingCards.Durak;

/// <summary>
///     Хранитель информации о картах.
/// </summary>
public static class CardsHolder
{
    private static List<Card>? _cards;

    public static IEnumerable<Card> Cards => _cards ??= GetCards();

    private static List<Card> GetCards()
    {
        List<CardRank> ranks = new()
        {
            new CardRank(6, "6"),
            new CardRank(7, "7"),
            new CardRank(8, "8"),
            new CardRank(9, "9"),
            new CardRank(10, "10"),
            new CardRank(11, "Jack"),
            new CardRank(12, "Queen"),
            new CardRank(13, "King"),
            new CardRank(14, "Ace")
        };

        List<CardSuit> suits = new()
        {
            new CardSuit(0, "Clubs", '♣'),
            new CardSuit(1, "Diamonds", '♦'),
            new CardSuit(2, "Hearts", '♥'),
            new CardSuit(3, "Spades", '♠')
        };

        return suits.SelectMany(_ => ranks, (suit, rank) => new Card(rank, suit)).ToList();
    }
}