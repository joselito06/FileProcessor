using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Models
{
    // Configuración para la búsqueda y procesamiento
    public class FileSearchConfig
    {
        public List<string> SearchPaths { get; set; } = new List<string>();
        public List<string> FileNames { get; set; } = new List<string>();
        public List<string> FilePatterns { get; set; } = new List<string>(); // *.xlsx, reporte_*.xlsx, etc.
        public TimeSpan ScheduledTime { get; set; } = new TimeSpan(8, 0, 0); // 8:00 AM por defecto
        public List<TimeSpan> ScheduledTimes { get; set; } = new List<TimeSpan>(); // Múltiples horarios
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(30); // 30 min por defecto
        public bool SearchUntilFound { get; set; } = true;
        public TimeSpan? StopSearchingAfter { get; set; } // Opcional: parar después de cierta hora
        public bool IncludeSubdirectories { get; set; } = false;
        public List<string> ExcludePatterns { get; set; } = new List<string> { "~$*", "temp_*" }; // Excluir archivos temporales
        public long? MaxFileSizeBytes { get; set; } // Límite de tamaño de archivo
        public TimeSpan? FileAge { get; set; } // Solo archivos más nuevos que X tiempo
        // NUEVO: Configuración de comportamiento para múltiples horarios
        public bool ProcessOncePerDay { get; set; } = true; // Si true, procesa solo una vez por día independientemente del horario
        public bool ProcessOnAllSchedules { get; set; } = false; // Si true, procesa en TODOS los horarios programados
    }
}
