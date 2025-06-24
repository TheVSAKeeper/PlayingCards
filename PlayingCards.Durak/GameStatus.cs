namespace PlayingCards.Durak;

/// <summary>
/// Статус игры.
/// </summary>
public enum GameStatus
{
    /// <summary>
    /// Мало игроков для начала игры.
    /// </summary>
    WaitPlayers = 0,

    /// <summary>
    /// Можно начинать.
    /// </summary>
    ReadyToStart = 1,

    /// <summary>
    /// В процессе.
    /// </summary>
    InProcess = 2,

    /// <summary>
    /// Объявился дурач☻к.
    /// </summary>
    Finish = 3,
}
