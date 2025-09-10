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
    public class MultiScheduleFileProcessorService : IFileProcessor, IDisposable
    {
        private readonly FileSearchConfig _config;
        private readonly ProcessFilesDelegate _processFilesTask;
        private readonly IFileSearchService _fileSearchService;
        private readonly MultiSchedulingService _schedulingService;
        private readonly IFileProcessorLogger _logger;

        // Control de procesamiento
        private readonly Dictionary<string, DateTime> _processedFilesToday = new();
        private readonly Dictionary<TimeSpan, DateTime> _lastExecutionBySchedule = new();
        private DateTime _currentDay = DateTime.Now.Date;
        private CancellationTokenSource _cancellationTokenSource;

        public bool IsRunning => _schedulingService?.IsRunning == true;
        public DateTime? NextScheduledExecution => _schedulingService?.NextExecution;

        // Eventos
        public event EventHandler<ProcessingEventArgs> OnProcessingStarted;
        public event EventHandler<ProcessingEventArgs> OnProcessingCompleted;
        public event EventHandler<ProcessingEventArgs> OnProcessingError;
        public event EventHandler<ProcessingEventArgs> OnFilesNotFound;
        public event EventHandler<ProcessingEventArgs> OnFilesFound;
        public event EventHandler<ProcessingEventArgs> OnRetryScheduled;

        public MultiScheduleFileProcessorService(
            FileSearchConfig config,
            ProcessFilesDelegate processFilesTask,
            IFileSearchService fileSearchService = null,
            IFileProcessorLogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _processFilesTask = processFilesTask ?? throw new ArgumentNullException(nameof(processFilesTask));
            _fileSearchService = fileSearchService ?? new FileSearchService();
            _schedulingService = new MultiSchedulingService();
            _logger = logger ?? new FileProcessorLogger();

            _cancellationTokenSource = new CancellationTokenSource();
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            if (!_config.SearchPaths.Any())
                throw new ArgumentException("Debe especificar al menos una ruta de búsqueda.");

            if (!_config.FileNames.Any() && !_config.FilePatterns.Any())
                throw new ArgumentException("Debe especificar al menos un nombre de archivo o patrón.");

            _logger.LogInfo("Configuración validada exitosamente.");
        }

        public void Start()
        {
            Stop();
            _cancellationTokenSource = new CancellationTokenSource();

            // Determinar si usar múltiples horarios o uno solo
            if (_config.ScheduledTimes.Any())
            {
                _logger.LogInfo($"Iniciando con {_config.ScheduledTimes.Count} horarios programados");
                _schedulingService.ScheduleMultipleTimes(_config.ScheduledTimes, () => ProcessFilesForSchedule());
            }
            else
            {
                _logger.LogInfo("Iniciando con horario único");
                _schedulingService.ScheduleDaily(_config.ScheduledTime, () => ProcessFilesForSchedule());
            }

            var message = $"Servicio iniciado. Próxima ejecución: {NextScheduledExecution}";
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

        private async Task<ProcessingResult> ProcessFilesForSchedule()
        {
            var currentSchedule = GetCurrentScheduleTime();
            _logger.LogInfo($"Ejecutando procesamiento programado para horario: {currentSchedule:hh\\:mm}");

            // Verificar si ya se ejecutó en este horario hoy
            if (ShouldSkipThisSchedule(currentSchedule))
            {
                var skipMessage = $"Ya se ejecutó en el horario {currentSchedule:hh\\:mm} hoy.";
                _logger.LogInfo(skipMessage);
                return new ProcessingResult { Success = true, ErrorMessage = skipMessage };
            }

            var result = await ProcessFiles();

            // Marcar este horario como ejecutado
            if (result.Success)
            {
                _lastExecutionBySchedule[currentSchedule] = DateTime.Now;
            }

            return result;
        }

        private async Task<ProcessingResult> ProcessFiles()
        {
            var startTime = DateTime.Now;

            try
            {
                CheckAndResetDailyTracking();

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

                // Determinar qué archivos procesar según la configuración
                var filesToProcess = DetermineFilesToProcess(foundFiles);

                if (!filesToProcess.Any())
                {
                    var skipMessage = GetSkipMessage(foundFiles);
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
                    Message = $"Encontrados {foundFiles.Count} archivos, {filesToProcess.Count} pendientes",
                    FoundFiles = foundFiles
                });

                var processedData = await _processFilesTask(filesToProcess);
                MarkFilesAsProcessed(filesToProcess);

                var result = CreateSuccessResult(foundFiles, filesToProcess, processedData, startTime);

                var successMessage = $"Procesamiento completado. Archivos procesados: {filesToProcess.Count}";
                _logger.LogInfo(successMessage);
                OnProcessingCompleted?.Invoke(this, new ProcessingEventArgs { Message = successMessage, Result = result });

                return result;
            }
            catch (Exception ex)
            {
                return HandleProcessingError(ex, startTime);
            }
        }

        private TimeSpan GetCurrentScheduleTime()
        {
            var now = DateTime.Now.TimeOfDay;

            if (_config.ScheduledTimes.Any())
            {
                // Encontrar el horario más cercano
                return _config.ScheduledTimes
                    .OrderBy(t => Math.Abs((t - now).TotalMinutes))
                    .First();
            }

            return _config.ScheduledTime;
        }

        private bool ShouldSkipThisSchedule(TimeSpan scheduleTime)
        {
            if (_config.ProcessOnAllSchedules)
                return false; // Procesar en todos los horarios

            if (_config.ProcessOncePerDay)
            {
                // Si ya se procesó hoy en cualquier horario, saltar
                return _lastExecutionBySchedule.Any(kvp => kvp.Value.Date == DateTime.Now.Date);
            }

            // Verificar si ya se ejecutó en este horario específico hoy
            return _lastExecutionBySchedule.TryGetValue(scheduleTime, out var lastExecution) &&
                   lastExecution.Date == DateTime.Now.Date;
        }

        private List<FileInfo> DetermineFilesToProcess(List<FileInfo> foundFiles)
        {
            if (_config.ProcessOnAllSchedules)
            {
                // Procesar todos los archivos en cada horario
                return foundFiles;
            }

            // Filtrar archivos ya procesados hoy
            return FilterAlreadyProcessedFiles(foundFiles);
        }

        private string GetSkipMessage(List<FileInfo> foundFiles)
        {
            if (_config.ProcessOncePerDay && _lastExecutionBySchedule.Any(kvp => kvp.Value.Date == DateTime.Now.Date))
            {
                return "Ya se procesó hoy (ProcessOncePerDay=true).";
            }

            return $"Todos los {foundFiles.Count} archivos ya fueron procesados hoy.";
        }

        private void CheckAndResetDailyTracking()
        {
            var today = DateTime.Now.Date;
            if (_currentDay != today)
            {
                _processedFilesToday.Clear();
                _lastExecutionBySchedule.Clear();
                _currentDay = today;
                _logger.LogInfo("Nuevo día detectado. Reiniciando tracking.");
            }
        }

        private List<FileInfo> FilterAlreadyProcessedFiles(List<FileInfo> foundFiles)
        {
            return foundFiles.Where(file =>
            {
                var fileKey = GetFileKey(file);
                return !_processedFilesToday.ContainsKey(fileKey);
            }).ToList();
        }

        private void MarkFilesAsProcessed(List<FileInfo> files)
        {
            var now = DateTime.Now;
            foreach (var file in files)
            {
                var fileKey = GetFileKey(file);
                _processedFilesToday[fileKey] = now;
            }
        }

        private string GetFileKey(FileInfo file)
        {
            return $"{file.FullPath}|{file.ModifiedAt:yyyyMMddHHmmss}";
        }

        private ProcessingResult CreateSuccessResult(List<FileInfo> foundFiles, List<FileInfo> processedFiles, object processedData, DateTime startTime)
        {
            var result = new ProcessingResult
            {
                Success = true,
                ProcessedFiles = processedFiles,
                ProcessedAt = DateTime.Now,
                Data = processedData,
                ProcessingDuration = DateTime.Now - startTime
            };

            result.Statistics["TotalFilesFound"] = foundFiles.Count;
            result.Statistics["FilesProcessedNow"] = processedFiles.Count;
            result.Statistics["FilesSkippedNow"] = foundFiles.Count - processedFiles.Count;
            result.Statistics["TotalProcessedToday"] = _processedFilesToday.Count;
            result.Statistics["ScheduledExecutionsToday"] = _lastExecutionBySchedule.Count;
            result.Statistics["TotalSizeBytes"] = processedFiles.Sum(f => f.SizeBytes);

            return result;
        }

        private ProcessingResult HandleProcessingError(Exception ex, DateTime startTime)
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

        private void ScheduleRetry()
        {
            if (_config.SearchUntilFound)
            {
                var now = DateTime.Now;

                if (_config.StopSearchingAfter.HasValue && now.TimeOfDay > _config.StopSearchingAfter.Value)
                {
                    var message = "Búsqueda detenida por límite de tiempo del día.";
                    _logger.LogInfo(message);
                    return;
                }

                _schedulingService.ScheduleRetry(_config.RetryInterval, ProcessFiles);

                var retryMessage = $"Reintento programado en {_config.RetryInterval.TotalMinutes} minutos.";
                _logger.LogInfo(retryMessage);
                OnRetryScheduled?.Invoke(this, new ProcessingEventArgs { Message = retryMessage });
            }
        }

        public Dictionary<string, object> GetDailyStatistics()
        {
            CheckAndResetDailyTracking();

            return new Dictionary<string, object>
            {
                ["ProcessedFilesToday"] = _processedFilesToday.Count,
                ["ScheduledExecutionsToday"] = _lastExecutionBySchedule.Count,
                ["ExecutionTimes"] = _lastExecutionBySchedule.ToDictionary(
                    kvp => kvp.Key.ToString(@"hh\:mm"),
                    kvp => kvp.Value.ToString("HH:mm:ss")),
                ["NextScheduledTimes"] = _config.ScheduledTimes.Select(t => t.ToString(@"hh\:mm")).ToList(),
                ["ProcessOncePerDay"] = _config.ProcessOncePerDay,
                ["ProcessOnAllSchedules"] = _config.ProcessOnAllSchedules,
                ["CurrentDay"] = _currentDay
            };
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
