using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Models
{
    public class ProcessingEventArgs : EventArgs
    {
        public string Message { get; set; }
        public ProcessingResult Result { get; set; }
        public Exception Exception { get; set; }
        public List<FileInfo> FoundFiles { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
