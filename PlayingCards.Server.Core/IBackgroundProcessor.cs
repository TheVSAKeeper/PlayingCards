namespace PlayingCards.Server.Core;

/// <summary>
/// Участник фонового тика (раз в секунду) из BackgroundExecutorService.
/// </summary>
public interface IBackgroundProcessor
{
    void BackgroundProcess();
}
