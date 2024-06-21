using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class DefenceModel : AuthModel
    {
        public Guid TableId { get; set; }

        public int DefenceCardIndex { get; set; }

        public int AttackCardIndex { get; set; }
    }
}
