namespace PlayingCards.Durak.Web.Business;

/// <summary>
/// Игрок за столом.
/// </summary>
public class TablePlayer
{
    /// <summary>
    /// Игрок.
    /// </summary>
    public Player Player { get; set; }

    /// <summary>
    /// Временная засечка, от которой будет считать АФК.
    /// </summary>
    public DateTime? AfkStartTime { get; set; }

    /// <summary>
    /// Секрет авторизации.
    /// </summary>
    public string AuthSecret { get; set; }
}
