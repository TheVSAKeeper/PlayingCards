namespace PlayingCards.Durak.Web.Models;

public class PlayerLeftNotification
{
    public string PlayerName { get; set; } = string.Empty;

    public Guid TableId { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}