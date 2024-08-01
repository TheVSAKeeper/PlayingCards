namespace PlayingCards.Durak.Web.Models;

public class PlayCardsModel : BaseTableModel
{
    public int[] CardIndexes { get; set; } = null!;

    public int? AttackCardIndex { get; set; }
}
