using ExcelFileProcessor.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace FileProcessor.Services
{
    public class MultiSchedulingService : ISchedulingService, IDisposable
    {
        private readonly List<Timer> _scheduledTimers = new List<Timer>();
        private Timer _retryTimer;
        private DateTime? _nextExecution;
        private Func<Task> _currentAction;
        private readonly List<TimeSpan> _scheduledTimes = new List<TimeSpan>();

        public bool IsRunning => _scheduledTimers.Any(t => t.Enabled) || _retryTimer?.Enabled == true;
        public DateTime? NextExecution => _nextExecution;

        // NUEVO: Programar múltiples horarios
        public void ScheduleMultipleTimes(List<TimeSpan> times, Func<Task> action)
        {
            Stop();
            _currentAction = action;
            _scheduledTimes.Clear();
            _scheduledTimes.AddRange(times.OrderBy(t => t));

            var now = DateTime.Now;
            Console.WriteLine($"🕐 Programando {times.Count} horarios diarios:");

            foreach (var time in _scheduledTimes)
            {
                ScheduleSingleTime(time, action);
            }

            // Establecer la próxima ejecución
            _nextExecution = GetNextScheduledExecution();
            Console.WriteLine($"⏰ Próxima ejecución: {_nextExecution:yyyy-MM-dd HH:mm:ss}");
        }

        // Método existente para compatibilidad
        public void ScheduleDaily(TimeSpan time, Func<Task> action)
        {
            ScheduleMultipleTimes(new List<TimeSpan> { time }, action);
        }

        private void ScheduleSingleTime(TimeSpan time, Func<Task> action)
        {
            var now = DateTime.Now;
            var scheduledToday = now.Date.Add(time);

            // Si ya pasó la hora de hoy, programar para mañana
            if (now > scheduledToday)
                scheduledToday = scheduledToday.AddDays(1);

            var timeUntilExecution = scheduledToday - now;
            Console.WriteLine($"   📅 {time:hh\\:mm} - en {timeUntilExecution.TotalMinutes:F1} minutos");

            var timer = new Timer(timeUntilExecution.TotalMilliseconds);
            timer.Elapsed += async (sender, e) =>
            {
                await ExecuteAction($"Scheduled-{time:hh\\:mm}");

                // Reprogramar para el siguiente día
                timer.Stop();
                timer.Dispose();
                _scheduledTimers.Remove(timer);

                ScheduleSingleTime(time, action);
            };
            timer.AutoReset = false;
            timer.Start();

            _scheduledTimers.Add(timer);
        }

        public void ScheduleRetry(TimeSpan interval, Func<Task> action)
        {
            _retryTimer?.Stop();
            _retryTimer?.Dispose();

            _currentAction = action;
            var nextRetry = DateTime.Now.Add(interval);

            Console.WriteLine($"🔄 Reintento programado para: {nextRetry:yyyy-MM-dd HH:mm:ss} (en {interval.TotalMinutes:F1} minutos)");

            _retryTimer = new Timer(interval.TotalMilliseconds);
            _retryTimer.Elapsed += async (sender, e) =>
            {
                _retryTimer?.Stop();
                await ExecuteAction("Retry");
            };
            _retryTimer.AutoReset = false;
            _retryTimer.Start();
        }

        private DateTime? GetNextScheduledExecution()
        {
            if (!_scheduledTimes.Any()) return null;

            var now = DateTime.Now;
            var today = now.Date;

            // Buscar la próxima ejecución hoy
            foreach (var time in _scheduledTimes)
            {
                var scheduledToday = today.Add(time);
                if (scheduledToday > now)
                {
                    return scheduledToday;
                }
            }

            // Si no hay más ejecuciones hoy, la primera de mañana
            var tomorrow = today.AddDays(1);
            return tomorrow.Add(_scheduledTimes.First());
        }

        private async Task ExecuteAction(string triggerType)
        {
            try
            {
                Console.WriteLine($"⚡ Ejecutando acción ({triggerType}) - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                if (_currentAction != null)
                {
                    await _currentAction();
                }

                // Actualizar próxima ejecución
                _nextExecution = GetNextScheduledExecution();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error en ejecución ({triggerType}): {ex.Message}");
            }
        }

        public void Stop()
        {
            foreach (var timer in _scheduledTimers)
            {
                timer.Stop();
                timer.Dispose();
            }
            _scheduledTimers.Clear();

            _retryTimer?.Stop();
            _retryTimer?.Dispose();
            _nextExecution = null;

            Console.WriteLine("🛑 Todos los horarios detenidos");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
