using Microsoft.AspNetCore.Mvc;

namespace PlayingCards.Durak.Web.Models
{
    public class StartGameModel : AuthModel
    {
        public Guid TableId { get; set; }
    }
}
