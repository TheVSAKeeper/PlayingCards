namespace PlayingCards.Durak.Tests;

public static class TestHelper
{
    /// <summary>
    ///     Превратить строку в индексы карт из руки и начать раунд.
    /// </summary>
    public static void StartAttack(this PlayerHand hand, string cardsString)
    {
        int[] result = GetHandCardIndexes(hand.Cards.ToList(), cardsString);
        hand.StartAttack(result);
    }

    /// <summary>
    ///     Превратить строку в индексы карт из руки и подкинуть.
    /// </summary>
    public static void Attack(this PlayerHand hand, string cardsString)
    {
        int[] result = GetHandCardIndexes(hand.Cards.ToList(), cardsString);
        hand.Attack(result);
    }

    /// <summary>
    ///     Превратить строку в индексы карт из руки и отбиться.
    /// </summary>
    public static void Defence(this PlayerHand hand, string cardsString)
    {
        int index = cardsString.IndexOf("->", StringComparison.InvariantCultureIgnoreCase);
        string defenceCardStr = cardsString[..index];
        string attackCardStr = cardsString[(index + 2)..];

        int defenceCardIndex = GetHandCardIndexes(hand.Cards.ToList(), defenceCardStr)[0];
        int attackCardIndex = GetHandCardIndexes(hand.Game.Cards.Select(tableCard => tableCard.AttackCard).ToList(), attackCardStr)[0];
        hand.Defence(defenceCardIndex, attackCardIndex);
    }

    private static int[] GetHandCardIndexes(List<Card> handCards, string cardsString)
    {
        string[] cardsSplit = cardsString.Split(' ');
        int[] result = new int[cardsSplit.Length];

        for (int i = 0; i < cardsSplit.Length; i++)
        {
            string cardString = cardsSplit[i];
            Card card = handCards.Single(handCard => handCard.ToString() == cardString);
            int index = handCards.IndexOf(card);
            result[i] = index;
        }

        return result;
    }
}