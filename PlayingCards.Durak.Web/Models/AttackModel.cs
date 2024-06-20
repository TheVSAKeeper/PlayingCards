using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class AttackModel : AuthModel
    {
        public Guid TableId { get; set; }

        public int[] CardIndexes { get; set; }
    }
}
