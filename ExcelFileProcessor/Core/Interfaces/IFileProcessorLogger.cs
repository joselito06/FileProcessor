using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Interfaces
{
    public interface IFileProcessorLogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception exception = null);
        void LogDebug(string message);
    }
}
