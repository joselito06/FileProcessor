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
    public class FileProcessorService : IFileProcessor, IDisposable
    {
        private readonly FileSearchConfig _config;
        private readonly ProcessFilesDelegate _processFilesTask;
        private readonly IFileSearchService _fileSearchService;
        private readonly ISchedulingService _schedulingService;
        private readonly IFileProcessorLogger _logger;

        private bool _hasProcessedToday = false;
        private DateTime _lastProcessedDate = DateTime.MinValue;
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

        public FileProcessorService(
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
            Stop(); // Detener cualquier timer existente

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
                // Verificar si ya se procesó hoy
                if (_hasProcessedToday && _lastProcessedDate.Date == DateTime.Now.Date)
                {
                    var skipMessage = "Ya se procesaron archivos hoy. Saltando ejecución.";
                    _logger.LogInfo(skipMessage);
                    return new ProcessingResult
                    {
                        Success = true,
                        ErrorMessage = skipMessage,
                        ProcessedAt = DateTime.Now,
                        ProcessingDuration = DateTime.Now - startTime
                    };
                }

                _logger.LogInfo("Buscando archivos...");
                var foundFiles = await _fileSearchService.FindFilesAsync(_config);

                if (!foundFiles.Any())
                {
                    var message = "No se encontraron archivos que coincidan con los criterios especificados.";
                    _logger.LogWarning(message);

                    OnFilesNotFound?.Invoke(this, new ProcessingEventArgs { Message = message });

                    if (_config.SearchUntilFound && !_hasProcessedToday)
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

                _logger.LogInfo($"Archivos encontrados: {foundFiles.Count}");
                OnFilesFound?.Invoke(this, new ProcessingEventArgs
                {
                    Message = $"Encontrados {foundFiles.Count} archivos para procesar",
                    FoundFiles = foundFiles
                });

                // Ejecutar la tarea personalizada del usuario
                _logger.LogInfo("Ejecutando tarea de procesamiento personalizada...");
                var processedData = await _processFilesTask(foundFiles);

                var result = new ProcessingResult
                {
                    Success = true,
                    ProcessedFiles = foundFiles,
                    ProcessedAt = DateTime.Now,
                    Data = processedData,
                    ProcessingDuration = DateTime.Now - startTime
                };

                // Agregar estadísticas
                result.Statistics["TotalFilesFound"] = foundFiles.Count;
                result.Statistics["TotalSizeBytes"] = foundFiles.Sum(f => f.SizeBytes);
                result.Statistics["AverageFileSizeBytes"] = foundFiles.Count > 0 ? foundFiles.Average(f => f.SizeBytes) : 0;
                result.Statistics["OldestFile"] = foundFiles.Count > 0 ? foundFiles.Min(f => f.ModifiedAt) : DateTime.MinValue;
                result.Statistics["NewestFile"] = foundFiles.Count > 0 ? foundFiles.Max(f => f.ModifiedAt) : DateTime.MinValue;

                _hasProcessedToday = true;
                _lastProcessedDate = DateTime.Now;

                var successMessage = $"Procesamiento completado exitosamente. Archivos procesados: {foundFiles.Count}";
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

                if (_config.SearchUntilFound && !_hasProcessedToday)
                {
                    ScheduleRetry();
                }

                return result;
            }
        }

        private void ScheduleRetry()
        {
            if (_config.SearchUntilFound && !_hasProcessedToday)
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

        public void Dispose()
        {
            Stop();
            _schedulingService?.Dispose();
            _cancellationTokenSource?.Dispose();
            _logger.LogInfo("Recursos liberados.");
        }
    }
}
