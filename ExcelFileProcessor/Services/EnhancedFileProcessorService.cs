using ExcelFileProcessor.Core.Delegates;
using ExcelFileProcessor.Core.Interfaces;
using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Services
{
    public class EnhancedFileProcessorService : IFileProcessor, IDisposable
    {
        private readonly FileSearchConfig _config;
        private readonly ProcessFilesDelegate _processFilesTask;
        private readonly IFileSearchService _fileSearchService;
        private readonly ISchedulingService _schedulingService;
        private readonly IFileProcessorLogger _logger;

        // Control mejorado por archivo individual
        private readonly Dictionary<string, DateTime> _processedFilesToday = new();
        private DateTime _currentDay = DateTime.Now.Date;
        private CancellationTokenSource _cancellationTokenSource;

        // Propiedades públicas
        public bool IsRunning => _schedulingService?.IsRunning == true;
        public DateTime? NextScheduledExecution => _schedulingService?.NextExecution;

        // Eventos
        public event EventHandler<ProcessingEventArgs> OnProcessingStarted;
        public event EventHandler<ProcessingEventArgs> OnProcessingCompleted;
        public event EventHandler<ProcessingEventArgs> OnProcessingError;
        public event EventHandler<ProcessingEventArgs> OnFilesNotFound;
        public event EventHandler<ProcessingEventArgs> OnFilesFound;
        public event EventHandler<ProcessingEventArgs> OnRetryScheduled;

        public EnhancedFileProcessorService(
            FileSearchConfig config,
            ProcessFilesDelegate processFilesTask,
            IFileSearchService fileSearchService = null,
            ISchedulingService schedulingService = null,
            IFileProcessorLogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _processFilesTask = processFilesTask ?? throw new ArgumentNullException(nameof(processFilesTask));
            _fileSearchService = fileSearchService ?? new FileSearchService();
            _schedulingService = schedulingService ?? new SchedulingService();
            _logger = logger ?? new FileProcessorLogger();

            _cancellationTokenSource = new CancellationTokenSource();
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (!_config.SearchPaths.Any())
            {
                var error = "Debe especificar al menos una ruta de búsqueda.";
                _logger.LogError(error);
                throw new ArgumentException(error);
            }

            if (!_config.FileNames.Any() && !_config.FilePatterns.Any())
            {
                var error = "Debe especificar al menos un nombre de archivo o patrón.";
                _logger.LogError(error);
                throw new ArgumentException(error);
            }

            _logger.LogInfo("Configuración validada exitosamente.");
        }

        public void Start()
        {
            Stop();
            _cancellationTokenSource = new CancellationTokenSource();

            // Configurar programación diaria
            _schedulingService.ScheduleDaily(_config.ScheduledTime, ProcessFiles);

            var message = $"Servicio iniciado. Próxima ejecución programada: {NextScheduledExecution}";
            _logger.LogInfo(message);
            OnProcessingStarted?.Invoke(this, new ProcessingEventArgs { Message = message });
        }

        public void Stop()
        {
            _schedulingService?.Stop();
            _cancellationTokenSource?.Cancel();

            var message = "Servicio detenido.";
            _logger.LogInfo(message);
            OnProcessingStarted?.Invoke(this, new ProcessingEventArgs { Message = message });
        }

        public async Task<List<FileInfo>> SearchFilesNow()
        {
            _logger.LogInfo("Buscando archivos...");
            return await _fileSearchService.FindFilesAsync(_config);
        }

        public async Task<ProcessingResult> ProcessNow()
        {
            _logger.LogInfo("Iniciando procesamiento inmediato...");
            return await ProcessFiles();
        }

        private async Task<ProcessingResult> ProcessFiles()
        {
            var startTime = DateTime.Now;

            try
            {
                // Limpiar registro si es un nuevo día
                CheckAndResetDailyTracking();

                _logger.LogInfo("Buscando archivos...");
                var foundFiles = await _fileSearchService.FindFilesAsync(_config);

                if (!foundFiles.Any())
                {
                    var message = "No se encontraron archivos que coincidan con los criterios especificados.";
                    _logger.LogWarning(message);

                    OnFilesNotFound?.Invoke(this, new ProcessingEventArgs { Message = message });

                    if (_config.SearchUntilFound)
                    {
                        ScheduleRetry();
                    }

                    return new ProcessingResult
                    {
                        Success = false,
                        ErrorMessage = message,
                        ProcessedAt = DateTime.Now,
                        ProcessingDuration = DateTime.Now - startTime
                    };
                }

                // Filtrar archivos ya procesados hoy
                var filesToProcess = FilterAlreadyProcessedFiles(foundFiles);

                if (!filesToProcess.Any())
                {
                    var skipMessage = $"Todos los {foundFiles.Count} archivos ya fueron procesados hoy.";
                    _logger.LogInfo(skipMessage);

                    return new ProcessingResult
                    {
                        Success = true,
                        ProcessedFiles = foundFiles,
                        ErrorMessage = skipMessage,
                        ProcessedAt = DateTime.Now,
                        ProcessingDuration = DateTime.Now - startTime
                    };
                }

                _logger.LogInfo($"Archivos encontrados: {foundFiles.Count}, Para procesar: {filesToProcess.Count}");
                OnFilesFound?.Invoke(this, new ProcessingEventArgs
                {
                    Message = $"Encontrados {foundFiles.Count} archivos, {filesToProcess.Count} pendientes de procesar",
                    FoundFiles = foundFiles
                });

                // Ejecutar la tarea personalizada del usuario
                _logger.LogInfo("Ejecutando tarea de procesamiento personalizada...");
                var processedData = await _processFilesTask(filesToProcess);

                // Marcar archivos como procesados
                MarkFilesAsProcessed(filesToProcess);

                var result = new ProcessingResult
                {
                    Success = true,
                    ProcessedFiles = filesToProcess,
                    ProcessedAt = DateTime.Now,
                    Data = processedData,
                    ProcessingDuration = DateTime.Now - startTime
                };

                // Agregar estadísticas
                result.Statistics["TotalFilesFound"] = foundFiles.Count;
                result.Statistics["FilesProcessedToday"] = filesToProcess.Count;
                result.Statistics["FilesSkippedToday"] = foundFiles.Count - filesToProcess.Count;
                result.Statistics["TotalSizeBytes"] = filesToProcess.Sum(f => f.SizeBytes);
                result.Statistics["AverageFileSizeBytes"] = filesToProcess.Count > 0 ? filesToProcess.Average(f => f.SizeBytes) : 0;

                var successMessage = $"Procesamiento completado exitosamente. Archivos procesados: {filesToProcess.Count}";
                _logger.LogInfo(successMessage);
                OnProcessingCompleted?.Invoke(this, new ProcessingEventArgs
                {
                    Message = successMessage,
                    Result = result
                });

                return result;
            }
            catch (Exception ex)
            {
                var result = new ProcessingResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ProcessedAt = DateTime.Now,
                    ProcessingDuration = DateTime.Now - startTime
                };

                var errorMessage = $"Error durante el procesamiento: {ex.Message}";
                _logger.LogError(errorMessage, ex);

                OnProcessingError?.Invoke(this, new ProcessingEventArgs
                {
                    Message = errorMessage,
                    Exception = ex,
                    Result = result
                });

                if (_config.SearchUntilFound)
                {
                    ScheduleRetry();
                }

                return result;
            }
        }

        private void CheckAndResetDailyTracking()
        {
            var today = DateTime.Now.Date;
            if (_currentDay != today)
            {
                _processedFilesToday.Clear();
                _currentDay = today;
                _logger.LogInfo("Nuevo día detectado. Reiniciando tracking de archivos procesados.");
            }
        }

        private List<FileInfo> FilterAlreadyProcessedFiles(List<FileInfo> foundFiles)
        {
            var filesToProcess = new List<FileInfo>();

            foreach (var file in foundFiles)
            {
                var fileKey = GetFileKey(file);

                if (_processedFilesToday.ContainsKey(fileKey))
                {
                    _logger.LogDebug($"Archivo ya procesado hoy: {file.FileName}");
                }
                else
                {
                    filesToProcess.Add(file);
                }
            }

            return filesToProcess;
        }

        private void MarkFilesAsProcessed(List<FileInfo> files)
        {
            var now = DateTime.Now;
            foreach (var file in files)
            {
                var fileKey = GetFileKey(file);
                _processedFilesToday[fileKey] = now;
                _logger.LogDebug($"Archivo marcado como procesado: {file.FileName}");
            }
        }

        private string GetFileKey(FileInfo file)
        {
            // Usar ruta completa + fecha de modificación para identificar archivos únicos
            // Esto permite reprocesar si el archivo cambia en el mismo día
            return $"{file.FullPath}|{file.ModifiedAt:yyyyMMddHHmmss}";
        }

        private void ScheduleRetry()
        {
            if (_config.SearchUntilFound)
            {
                var now = DateTime.Now;

                // Verificar límite de tiempo si está configurado
                if (_config.StopSearchingAfter.HasValue &&
                    now.TimeOfDay > _config.StopSearchingAfter.Value)
                {
                    var message = "Búsqueda detenida por límite de tiempo del día.";
                    _logger.LogInfo(message);
                    OnProcessingStarted?.Invoke(this, new ProcessingEventArgs { Message = message });
                    return;
                }

                _schedulingService.ScheduleRetry(_config.RetryInterval, ProcessFiles);

                var retryMessage = $"Reintento programado en {_config.RetryInterval.TotalMinutes} minutos.";
                _logger.LogInfo(retryMessage);
                OnRetryScheduled?.Invoke(this, new ProcessingEventArgs { Message = retryMessage });
            }
        }

        // Método para obtener estadísticas del día
        public Dictionary<string, object> GetDailyStatistics()
        {
            CheckAndResetDailyTracking();

            return new Dictionary<string, object>
            {
                ["ProcessedFilesToday"] = _processedFilesToday.Count,
                ["LastProcessingTime"] = _processedFilesToday.Values.DefaultIfEmpty(DateTime.MinValue).Max(),
                ["ProcessedFiles"] = _processedFilesToday.Keys.Select(key => key.Split('|')[0]).ToList(),
                ["CurrentDay"] = _currentDay
            };
        }

        // Método para resetear el tracking manualmente
        public void ResetDailyTracking()
        {
            _processedFilesToday.Clear();
            _logger.LogInfo("Tracking de archivos procesados reiniciado manualmente.");
        }

        public void Dispose()
        {
            Stop();
            _schedulingService?.Dispose();
            _cancellationTokenSource?.Dispose();
            _logger.LogInfo("Recursos liberados.");
        }
    }
}
