using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class TakeModel : AuthModel
    {
        public Guid TableId { get; set; }
    }
}
