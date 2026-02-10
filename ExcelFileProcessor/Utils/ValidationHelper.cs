using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Utils
{
    public class ValidationHelper
    {
        public static List<string> ValidateFileSearchConfig(FileSearchConfig config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("La configuración no puede ser null");
                return errors;
            }

            if (!config.SearchPaths?.Any() == true)
            {
                errors.Add("Debe especificar al menos una ruta de búsqueda");
            }
            else
            {
                foreach (var path in config.SearchPaths)
                {
                    // Verificar si la ruta contiene tokens de fecha
                    bool hasDateToken = path.Contains("{date:");

                    // Si tiene búsqueda basada en fechas habilitada o tokens de fecha, ser más permisivo
                    if (config.EnableDateBasedSearch || hasDateToken)
                    {
                        // Extraer la ruta base para validación
                        var basePath = path;
                        if (hasDateToken)
                        {
                            var tokenStart = path.IndexOf("{date:");
                            if (tokenStart > 0)
                            {
                                basePath = path.Substring(0, tokenStart).TrimEnd('\\', '/');
                            }
                        }

                        // Validar solo que la ruta base tenga formato válido
                        if (!string.IsNullOrWhiteSpace(basePath))
                        {
                            if (!PathHelper.IsValidPath(basePath))
                            {
                                errors.Add($"Ruta base inválida: {basePath}");
                            }
                            // NO validar que exista - se expandirá dinámicamente con fechas
                        }
                    }
                    else
                    {
                        // Validación normal para rutas estáticas
                        if (!PathHelper.IsValidPath(path))
                        {
                            errors.Add($"Ruta inválida: {path}");
                        }
                        else if (!Directory.Exists(path))
                        {
                            errors.Add($"La ruta no existe: {path}");
                        }
                    }
                }
            }

            if (!config.FileNames?.Any() == true && !config.FilePatterns?.Any() == true)
            {
                errors.Add("Debe especificar al menos un nombre de archivo o patrón");
            }

            if (config.RetryInterval <= TimeSpan.Zero)
            {
                errors.Add("El intervalo de reintento debe ser mayor a cero");
            }

            if (config.ScheduledTime < TimeSpan.Zero || config.ScheduledTime >= TimeSpan.FromDays(1))
            {
                errors.Add("La hora programada debe estar entre 00:00:00 y 23:59:59");
            }

            if (config.MaxFileSizeBytes.HasValue && config.MaxFileSizeBytes <= 0)
            {
                errors.Add("El tamaño máximo de archivo debe ser mayor a cero");
            }

            if (config.FileAge.HasValue && config.FileAge <= TimeSpan.Zero)
            {
                errors.Add("La edad de archivo debe ser mayor a cero");
            }

            return errors;
        }

        public static bool IsValidFile(string filePath, out string error)
        {
            error = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                error = "La ruta del archivo no puede estar vacía";
                return false;
            }

            if (!File.Exists(filePath))
            {
                error = "El archivo no existe";
                return false;
            }

            try
            {
                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    error = "El archivo está vacío";
                    return false;
                }
            }
            catch (Exception ex)
            {
                error = $"Error al acceder al archivo: {ex.Message}";
                return false;
            }

            return true;
        }

        public static async Task<ValidationResult> ValidateFilesAsync(List<FileInfo> files)
        {
            var result = new ValidationResult();

            foreach (var file in files)
            {
                if (IsValidFile(file.FullPath, out var error))
                {
                    result.ValidFiles++;
                }
                else
                {
                    result.InvalidFiles++;
                    result.Errors.Add($"{file.FileName}: {error}");
                }
            }

            result.ValidationRate = files.Count > 0
                ? (result.ValidFiles * 100.0 / files.Count)
                : 0;

            return await Task.FromResult(result);
        }

        public static bool IsFileAccessible(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<string> ValidatePatterns(List<string> patterns)
        {
            var errors = new List<string>();

            foreach (var pattern in patterns)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    errors.Add("Los patrones no pueden estar vacíos");
                    continue;
                }

                try
                {
                    // Intentar usar el patrón en una búsqueda de prueba
                    var testDir = Path.GetTempPath();
                    Directory.GetFiles(testDir, pattern, SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    errors.Add($"Patrón inválido '{pattern}': {ex.Message}");
                }
            }

            return errors;
        }
    }

    public class ValidationResult
    {
        public int ValidFiles { get; set; }
        public int InvalidFiles { get; set; }
        public double ValidationRate { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}
