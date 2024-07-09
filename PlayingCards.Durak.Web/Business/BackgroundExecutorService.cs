using System.Timers;
using Microsoft.AspNetCore.SignalR;
using PlayingCards.Durak.Web.SignalR.Hubs;

namespace PlayingCards.Durak.Web.Business
{
    public class BackgroundExecutorService : IHostedService, IDisposable
    {
        private ILogger<BackgroundExecutorService> _logger;
        private TableHolder _tableHolder;
        private System.Timers.Timer _timer;
        private readonly IHubContext<GameHub> _hubContext;

        public BackgroundExecutorService(
            IHubContext<GameHub> hubContext,
            ILogger<BackgroundExecutorService> logger,
            TableHolder tableHolder)
        {
            _logger = logger;
            _tableHolder = tableHolder;
            _hubContext = hubContext;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"запущен!");
            _timer = new System.Timers.Timer();
            _timer.Interval = 1000;
            _timer.Elapsed += _timer_Elapsed;
            _timer.Start();
        }

        private async void _timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            var hasChanges = _tableHolder.BackgroundProcess();
            if (hasChanges)
            {
                await _hubContext.Clients.All.SendAsync("ChangeStatus");
            }
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
