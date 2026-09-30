namespace PlayingCards.Durak.Server;

/// <summary>
/// Игрок за столом.
/// </summary>
public class TablePlayer : TablePlayerBase
{
    /// <summary>
    /// Игрок.
    /// </summary>
    public Player Player { get; set; } = null!;

    /// <summary>
    /// Последняя реплика игрока («Бито!» при закрытии раунда) – всплывает над бейджем (issue F5).
    /// </summary>
    public string? Reply { get; set; }

    /// <summary>
    /// Время реплики <see cref="Reply" />; по нему вид решает, показывать ли её ещё (живёт пару секунд).
    /// </summary>
    public DateTime? ReplyDate { get; set; }

    /// <summary>
    /// Игрок сказал «Бито» в текущем окне остановки раунда (issue F5). Раунд закрывается досрочно,
    /// лишь когда это сделали все атакующие; сбрасывается на каждом новом окне.
    /// </summary>
    public bool SaidBeat { get; set; }
}
