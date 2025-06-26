using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.Hubs;
using PlayingCards.Durak.Web.Models;

namespace PlayingCards.Durak.Web.Business;

public class BackgroundExecutorService(ILogger<BackgroundExecutorService> logger, TableHolder tableHolder, IHubContext<GameHub>? hubContext = null) : BackgroundService
{
    private readonly PeriodicTimer _timer = new(TimeSpan.FromSeconds(1));

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _timer.Dispose();
        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Name} is starting.", nameof(BackgroundExecutorService));

        stoppingToken.Register(() => logger.LogInformation("{Name} is stopping.", nameof(BackgroundExecutorService)));

        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            await tableHolder.BackgroundProcess();

            await BroadcastTimerUpdates();
        }
    }

    private static int CalculateRemainingAfkSeconds(DateTime afkStartTime)
    {
        var elapsed = DateTime.UtcNow - afkStartTime;
        var remaining = TimeSpan.FromSeconds(TableHolder.AFK_SECONDS) - elapsed;
        return Math.Max(0, (int)remaining.TotalSeconds);
    }

    private static int CalculateRemainingStopRoundSeconds(DateTime stopRoundBeginDate, StopRoundStatus? stopRoundStatus)
    {
        if (stopRoundStatus == null)
        {
            return 0;
        }

        var finishTime = TableHolder.GetSecond(stopRoundBeginDate, stopRoundStatus);
        var remaining = finishTime - DateTime.UtcNow;
        return Math.Max(0, (int)remaining.TotalSeconds);
    }

    private async Task BroadcastTimerUpdates()
    {
        if (hubContext == null)
        {
            return;
        }

        try
        {
            var tables = tableHolder.GetTables();

            foreach (var table in tables)
            {
                var groupName = $"Table_{table.Id}";

                foreach (var player in table.Players)
                {
                    if (player.AfkStartTime == null)
                    {
                        continue;
                    }

                    var remainingSeconds = CalculateRemainingAfkSeconds(player.AfkStartTime.Value);

                    if (remainingSeconds < 0)
                    {
                        continue;
                    }

                    var timerUpdate = new TimerUpdate
                    {
                        Type = TimerType.Afk,
                        RemainingSeconds = remainingSeconds,
                        TableId = table.Id,
                        PlayerSecret = player.AuthSecret,
                    };

                    await hubContext.Clients.Group(groupName).SendAsync("TimerUpdate", timerUpdate);
                }

                if (table is not { StopRoundBeginDate: not null, StopRoundStatus: not null })
                {
                    continue;
                }

                {
                    var remainingSeconds = CalculateRemainingStopRoundSeconds(table.StopRoundBeginDate.Value, table.StopRoundStatus);

                    if (remainingSeconds < 0)
                    {
                        continue;
                    }

                    var timerUpdate = new TimerUpdate
                    {
                        Type = TimerType.StopRound,
                        RemainingSeconds = remainingSeconds,
                        TableId = table.Id,
                    };

                    await hubContext.Clients.Group(groupName).SendAsync("TimerUpdate", timerUpdate);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to broadcast timer updates");
        }
    }
}
