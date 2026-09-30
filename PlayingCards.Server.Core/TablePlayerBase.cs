namespace PlayingCards.Server.Core;

/// <summary>
/// Игрок за столом.
/// </summary>
public abstract class TablePlayerBase
{
    /// <summary>
    /// Секрет авторизации.
    /// </summary>
    public string AuthSecret { get; set; } = null!;

    /// <summary>
    /// Временная засечка, от которой будет считать АФК.
    /// </summary>
    public DateTime? AfkStartTime { get; set; }

    /// <summary>
    /// Игрок управляется ИИ-болванчиком (для отладки), а не человеком.
    /// </summary>
    public bool IsBot { get; set; }
}
