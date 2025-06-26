namespace PlayingCards.Durak.Web.Models;

public class TimerUpdate
{
    public TimerType Type { get; set; }

    public int RemainingSeconds { get; set; }

    public Guid TableId { get; set; }

    public string? PlayerSecret { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}