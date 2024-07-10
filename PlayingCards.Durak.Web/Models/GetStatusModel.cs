

namespace PlayingCards.Durak.Web.Models
{
    public class GetStatusModel
    {
        public TableModel? Table { get; set; }

        public TableModel[]? Tables { get; set; }

        public class TableModel
        {
            public Guid Id { get; set; }

            public int DeckCardsCount { get; set; }

            public CardModel[]? MyCards { get; set; }

            public CardModel? Trump { get; set; }

            public TableCardModel[]? Cards { get; set; }

            public PlayerModel[] Players { get; set; }

            public int MyPlayerIndex { get; set; }

            public int? ActivePlayerIndex { get; set; }

            public int? DefencePlayerIndex { get; set; }

            public int? LooserPlayerIndex { get; set; }

            public int Status { get; set; }

            public int OwnerIndex { get; set; }

            public int? StopRoundStatus { get; set; }

            public DateTime? StopRoundEndDate { get; set; }

            public int? NeedShowCardMinTrumpValue { get; set; }

            public PlayerModel? LeavePlayer { get; set; }

            public DateTime? AfkEndTime { get; set; }
        }

        public class CardModel
        {
            public CardModel(Card card)
            {
                Rank = card.Rank.Value;
                Suit = card.Suit.Value;
            }

            public int Rank { get; set; }

            public int Suit { get; set; }
        }

        public class TableCardModel
        {
            public CardModel? AttackCard { get; set; }

            public CardModel? DefenceCard { get; set; }
        }

        public class PlayerModel
        {
            public int Index { get; set; }

            public string Name { get; set; }

            public int CardsCount { get; set; }

            public DateTime? AfkEndTime { get; set; }
        }
    }
}
