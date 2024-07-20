namespace PlayingCards.Durak.Tests
{
    public static class TestHelper
    {
        /// <summary>
        /// Превратить строку в индексы карт из руки и начать раунд.
        /// </summary>
        public static void StartAttack(this PlayerHand hand, string cardsString)
        {
            int[] result = GetHandCardIndexes(hand.Cards, cardsString);
            hand.StartAttack(result);
        }

        /// <summary>
        /// Превратить строку в индексы карт из руки и подкинуть.
        /// </summary>
        public static void Attack(this PlayerHand hand, string cardsString)
        {
            int[] result = GetHandCardIndexes(hand.Cards, cardsString);
            hand.Attack(result);
        }

        /// <summary>
        /// Превратить строку в индексы карт из руки и подкинуть.
        /// </summary>
        public static void Defence(this PlayerHand hand, string cardsString)
        {
            var index = cardsString.IndexOf("->");
            var defenceCardStr = cardsString.Substring(0, index);
            var attackCardStr = cardsString.Substring(index + 2);

            int defenceCardIndex = GetHandCardIndexes(hand.Cards, defenceCardStr)[0];
            int attackCardIndex = GetHandCardIndexes(hand.Game.Cards.Select(x => x.AttackCard).ToList(), attackCardStr)[0];
            hand.Defence(defenceCardIndex, attackCardIndex);
        }

        private static int[] GetHandCardIndexes(List<Card> cards, string cardsString)
        {
            var cardsSplit = cardsString.Split(' ');
            var result = new int[cardsSplit.Length];
            for (int i = 0; i < cardsSplit.Length; i++)
            {
                var cardString = cardsSplit[i];
                var card = cards.Single(x => x.ToString() == cardString);
                var index = cards.IndexOf(card);
                result[i] = index;
            }

            return result;
        }
    }
}
