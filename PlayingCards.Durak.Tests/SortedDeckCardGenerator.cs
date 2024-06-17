using System.Collections.Generic;

namespace PlayingCards.Durak.Tests
{
    public class SortedDeckCardGenerator : RandomDeckCardGenerator
    {
        private string[] _cardValues;
        private string _trumpValue;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cardValues">
        /// Массив карт, которые должны попасть в руки игрокам.
        /// Количесто элементов массива должно равняться количеству игроков.
        /// Пример массива string [] { "A♠ 10♠ 6♠ J♥ W♥ Q♦", "K♠ 9♠ A♥ 10♥ 6♥ J♦"}.
        /// </param>
        /// <param name="trumpValue">
        /// Козырь. В формате "6♦"
        /// </param>
        public SortedDeckCardGenerator(string[] cardValues, string trumpValue)
        {
            _cardValues = cardValues;
            _trumpValue = trumpValue;
        }

        public override List<Card> GetCards()
        {
            var playerCount = _cardValues.Length;
            var deckCards = CardsHolder.GetCards();
            var returnCards = new List<Card>();
            var playerHands = new List<Card>[playerCount];
            for (int i = 0; i < _cardValues.Length; i++)
            {
                string? cardValues = _cardValues[i];
                var hand = new List<Card>();
                var cards = cardValues.Split(' ');
                foreach (var card in cards)
                {
                    var deckCard = GetCard(deckCards, card);
                    hand.Add(deckCard);
                    playerHands[i] = hand;
                }
            }

            for (var j = 0; j < 6; j++)
            {
                for (var i = 0; i < playerCount; i++)
                {
                    returnCards.Insert(0, playerHands[i][j]);
                }
            }

            var trumpCard = GetCard(deckCards, _trumpValue);
            returnCards.Insert(0, trumpCard);
            returnCards.InsertRange(1, deckCards);

            return returnCards;
        }

        private Card GetCard(List<Card> deckCards, string card)
        {
            var suit = card.Substring(card.Length - 1, 1)[0];
            var rank = card.Substring(0, card.Length - 1);
            if (!Int32.TryParse(rank, out int rankValue))
            {
                switch (rank)
                {
                    case "A":
                        rankValue = 14;
                        break;
                    case "K":
                        rankValue = 13;
                        break;
                    case "Q":
                        rankValue = 12;
                        break;
                    case "J":
                        rankValue = 11;
                        break;
                    default:
                        throw new Exception(rank + " rank undefinded");
                }
            }
            var deckCard = deckCards.FirstOrDefault(x => x.Rank.Value == rankValue && x.Suit.IconChar == suit);
            if (deckCard == null)
            {
                throw new Exception(card + " not found");
            }
            deckCards.Remove(deckCard);

            return deckCard;
        }
    }
}
