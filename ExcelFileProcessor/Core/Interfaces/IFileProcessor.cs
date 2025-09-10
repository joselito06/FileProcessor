using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Interfaces
{
    public interface IFileProcessor
    {
        void Start();
        void Stop();
        Task<ProcessingResult> ProcessNow();
        Task<List<Models.FileInfo>> SearchFilesNow();
        bool IsRunning { get; }
        DateTime? NextScheduledExecution { get; }
        void Dispose();

        event EventHandler<ProcessingEventArgs> OnProcessingStarted;
        event EventHandler<ProcessingEventArgs> OnProcessingCompleted;
        event EventHandler<ProcessingEventArgs> OnProcessingError;
        event EventHandler<ProcessingEventArgs> OnFilesNotFound;
        event EventHandler<ProcessingEventArgs> OnFilesFound;
        event EventHandler<ProcessingEventArgs> OnRetryScheduled;
    }
}
