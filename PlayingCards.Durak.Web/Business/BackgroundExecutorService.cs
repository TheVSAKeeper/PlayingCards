namespace PlayingCards.Durak.Web.Business;

public class BackgroundExecutorService(ILogger<BackgroundExecutorService> logger, TableHolder tableHolder) : BackgroundService
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
            tableHolder.BackgroundProcess();
        }
    }
}
