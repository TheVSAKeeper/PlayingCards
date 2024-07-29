namespace PlayingCards.Durak.Web.Business;

public class BackgroundExecutorService : BackgroundService
{
    private readonly ILogger<BackgroundExecutorService> _logger;
    private readonly PeriodicTimer _timer;
    private readonly TableHolder _tableHolder;

    public BackgroundExecutorService(ILogger<BackgroundExecutorService> logger, TableHolder tableHolder)
    {
        _logger = logger;
        _tableHolder = tableHolder;
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{Name} is starting.", nameof(BackgroundExecutorService));

        stoppingToken.Register(() => _logger.LogInformation("{Name} is stopping.", nameof(BackgroundExecutorService)));

        while (await _timer.WaitForNextTickAsync(stoppingToken))
        {
            _tableHolder.BackgroundProcess();
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _timer.Dispose();
        await base.StopAsync(stoppingToken);
    }
}