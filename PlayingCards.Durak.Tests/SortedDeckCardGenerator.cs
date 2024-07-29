namespace PlayingCards.Durak.Tests;

public class SortedDeckCardGenerator : RandomDeckCardGenerator
{
    private readonly int _skipCardCount;
    private readonly string? _deckValues;
    private readonly string? _trumpValue;
    private readonly string[] _cardValues;

    /// <summary>
    /// </summary>
    /// <param name="cardValues">
    ///     Массив карт, которые должны попасть в руки игрокам.
    ///     Количество элементов массива должно равняться количеству игроков.
    ///     Пример массива string [] { "A♠ 10♠ 6♠ J♥ W♥ Q♦", "K♠ 9♠ A♥ 10♥ 6♥ J♦"}.
    /// </param>
    /// <param name="trumpValue">
    ///     Козырь. В формате "6♦"
    /// </param>
    /// <param name="skipCardCount">
    ///     Сколько кард из колоды, сразу выкинуть в отбой.
    /// </param>
    /// <param name="deckValues">
    ///     Массив карт, которые останутся в колоде. (если заполнен, то trumpValue и skipCardCount игнорируются)
    ///     Пример массива { "A♠ 10♠ 6♠ J♥ W♥ Q♦" }.
    /// </param>
    public SortedDeckCardGenerator(string[] cardValues, string? trumpValue = null, int skipCardCount = 0, string? deckValues = null)
    {
        _cardValues = cardValues;
        _trumpValue = trumpValue;
        _skipCardCount = skipCardCount;
        _deckValues = deckValues;
    }

    public override List<Card> GetCards()
    {
        int playerCount = _cardValues.Length;
        List<Card> deckCards = CardsHolder.Cards.ToList();
        List<Card> returnCards = [];
        List<Card>[] playerHands = new List<Card>[playerCount];

        for (int i = 0; i < _cardValues.Length; i++)
        {
            string cardValues = _cardValues[i];
            List<Card> hand = [];
            string[] cards = cardValues.Split(' ');

            foreach (string card in cards)
            {
                Card deckCard = GetCard(deckCards, card);
                hand.Add(deckCard);
                playerHands[i] = hand;
            }
        }

        for (int j = 0; j < 6; j++)
        {
            for (int i = 0; i < playerCount; i++)
            {
                returnCards.Insert(0, playerHands[i][j]);
            }
        }

        if (_deckValues != null)
        {
            string[] cards = _deckValues.Split(' ');
            List<Card> newDeckCards = [..cards.Select(card => GetCard(deckCards, card))];
            returnCards.InsertRange(0, newDeckCards);
        }
        else
        {
            Card? trumpCard = _trumpValue == null ? null : GetCard(deckCards, _trumpValue);

            if (_skipCardCount > 0)
            {
                deckCards = deckCards.Skip(_skipCardCount).ToList();
            }

            returnCards.InsertRange(0, deckCards);

            if (trumpCard != null)
            {
                returnCards.Insert(0, trumpCard);
            }
        }

        return returnCards;
    }

    private Card GetCard(List<Card> deckCards, string card)
    {
        char suit = card.Substring(card.Length - 1, 1)[0];
        string rank = card[..^1];

        if (int.TryParse(rank, out int rankValue) == false)
        {
            rankValue = rank switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                var _ => throw new Exception($"{rank} rank undefined")
            };
        }

        Card? deckCard = deckCards.FirstOrDefault(x => x.Rank.Value == rankValue && x.Suit.IconChar == suit);

        if (deckCard == null)
        {
            throw new Exception($"{card} not found");
        }

        deckCards.Remove(deckCard);

        return deckCard;
    }
}