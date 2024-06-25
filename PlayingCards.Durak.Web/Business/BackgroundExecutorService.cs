using System.Timers;

namespace PlayingCards.Durak.Web.Business
{
    public class BackgroundExecutorService : IHostedService, IDisposable
    {
        private ILogger<BackgroundExecutorService> _logger;
        private TableHolder _tableHolder;
        private System.Timers.Timer _timer;

        public BackgroundExecutorService(ILogger<BackgroundExecutorService> logger, TableHolder tableHolder)
        {
            _logger = logger;
            _tableHolder = tableHolder;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"запущен!");
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            _tableHolder.CheckStopRound();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer.Stop();
            _logger.LogInformation($"остановлен!");
        }

        public void Dispose()
        {
        }
    }

}
