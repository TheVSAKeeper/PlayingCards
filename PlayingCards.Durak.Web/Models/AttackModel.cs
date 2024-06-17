using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class AttackModel
    {
        public string PlayerSecret { get; set; }

        public string PlayerName { get; set; }

        public Guid TableId { get; set; }

        public int[] CardIndexes { get; set; }
    }
}
