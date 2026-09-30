using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PlayingCards.Server.Core;

public class BackgroundExecutorService(ILogger<BackgroundExecutorService> logger, IEnumerable<IBackgroundProcessor> processors) : BackgroundService
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
            foreach (var processor in processors)
            {
                processor.BackgroundProcess();
            }
        }
    }
}
