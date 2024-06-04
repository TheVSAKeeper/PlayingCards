using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class JoinModel
    {
        public string PlayerSecret { get; set; }

        public string PlayerName { get; set; }

        public Guid TableId { get; set; }
    }
}
