using ExcelFileProcessor.Core.Interfaces;
using System.Threading.Tasks;
using System.Timers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Timer = System.Timers.Timer;

namespace FileProcessor.Services
{
    public class SchedulingService : ISchedulingService, IDisposable
    {
        private Timer _dailyTimer;
        private Timer _retryTimer;
        private DateTime? _nextExecution;

        public bool IsRunning => _dailyTimer?.Enabled == true || _retryTimer?.Enabled == true;
        public DateTime? NextExecution => _nextExecution;

        public void ScheduleDaily(TimeSpan time, Func<Task> action)
        {
            Stop();

            var now = DateTime.Now;
            var scheduledToday = now.Date.Add(time);

            if (now > scheduledToday)
                scheduledToday = scheduledToday.AddDays(1);

            _nextExecution = scheduledToday;
            var timeUntilExecution = scheduledToday - now;

            _dailyTimer = new Timer(timeUntilExecution.TotalMilliseconds);
            _dailyTimer.Elapsed += async (sender, e) =>
            {
                await action();
                ScheduleDaily(time, action); // Reprogramar para el siguiente día
            };
            _dailyTimer.AutoReset = false;
            _dailyTimer.Start();
        }

        public void ScheduleRetry(TimeSpan interval, Func<Task> action)
        {
            _retryTimer?.Stop();
            _retryTimer?.Dispose();

            _retryTimer = new Timer(interval.TotalMilliseconds);
            _retryTimer.Elapsed += async (sender, e) => await action();
            _retryTimer.AutoReset = false;
            _retryTimer.Start();
        }

        public void Stop()
        {
            _dailyTimer?.Stop();
            _dailyTimer?.Dispose();
            _retryTimer?.Stop();
            _retryTimer?.Dispose();
            _nextExecution = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
