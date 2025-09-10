using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Interfaces
{
    public interface ISchedulingService
    {
        void ScheduleDaily(TimeSpan time, Func<Task> action);
        void ScheduleRetry(TimeSpan interval, Func<Task> action);
        void Stop();
        bool IsRunning { get; }
        DateTime? NextExecution { get; }
        void Dispose();
    }
}
