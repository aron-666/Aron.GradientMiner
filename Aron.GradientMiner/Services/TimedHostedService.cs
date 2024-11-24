   using Microsoft.Extensions.Hosting;
   using System;
   using System.Threading;
   using System.Threading.Tasks;

namespace Aron.GradientMiner.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private Timer _timer;
        public int Interval { get; }
        public TimedHostedService(int interval)
        {
            Interval = interval;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(Execute, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(Interval));
            return Task.CompletedTask;
        }

        private void Execute(object state)
        {
            // 執行任務的邏輯
            Console.WriteLine("任務執行中...");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

    }

}
