using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Models
{
    // Resultado del procesamiento
    public class ProcessingResult
    {
        public bool Success { get; set; }
        public List<FileInfo> ProcessedFiles { get; set; } = new List<FileInfo>();
        public string ErrorMessage { get; set; }
        public DateTime ProcessedAt { get; set; }
        public object Data { get; set; } // Datos devueltos por la tarea personalizada
        public TimeSpan ProcessingDuration { get; set; }
        public Dictionary<string, object> Statistics { get; set; } = new Dictionary<string, object>();
    }

}
