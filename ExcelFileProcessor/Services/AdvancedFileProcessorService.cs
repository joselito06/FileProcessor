using ExcelFileProcessor.Core.Delegates;
using ExcelFileProcessor.Core.Interfaces;
using ExcelFileProcessor.Core.Models;
using FileProcessor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Services
{
    /// <summary>
    /// Servicio avanzado de procesamiento con soporte para:
    /// - Múltiples horarios programados
    /// - Búsqueda de carpetas por fecha
    /// - Ejecución manual por señal/evento
    /// </summary>
    public class AdvancedFileProcessorService : IFileProcessor, IDisposable
    {
        private readonly FileSearchConfig _config;
        private readonly ProcessFilesDelegate _processFilesTask;
        private readonly IFileSearchService _fileSearchService;
        private readonly MultiSchedulingService _multiSchedulingService;
        private readonly IFileProcessorLogger _logger;

        // Control de procesamiento
        private readonly Dictionary<string, DateTime> _processedFilesToday = new();
        private readonly Dictionary<TimeSpan, DateTime> _lastExecutionBySchedule = new();
        private DateTime _currentDay = DateTime.Now.Date;
        private CancellationTokenSource _cancellationTokenSource;

        // Para ejecución manual/por señal
        private readonly SemaphoreSlim _manualExecutionSemaphore = new SemaphoreSlim(1, 1);
        private readonly ManualResetEventSlim _manualTrigger = new ManualResetEventSlim(false);
        private Task _manualExecutionTask;

        public bool IsRunning => _multiSchedulingService?.IsRunning == true;
        public DateTime? NextScheduledExecution => _multiSchedulingService?.NextExecution;

        // Eventos
        public event EventHandler<ProcessingEventArgs> OnProcessingStarted;
        public event EventHandler<ProcessingEventArgs> OnProcessingCompleted;
        public event EventHandler<ProcessingEventArgs> OnProcessingError;
        public event EventHandler<ProcessingEventArgs> OnFilesNotFound;
        public event EventHandler<ProcessingEventArgs> OnFilesFound;
        public event EventHandler<ProcessingEventArgs> OnRetryScheduled;
        public event EventHandler<ProcessingEventArgs> OnManualExecutionTriggered;

        public AdvancedFileProcessorService(
            FileSearchConfig config,
            ProcessFilesDelegate processFilesTask,
            IFileSearchService fileSearchService = null,
            IFileProcessorLogger logger = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _processFilesTask = processFilesTask ?? throw new ArgumentNullException(nameof(processFilesTask));
            _fileSearchService = fileSearchService ?? new FileSearchService();
            _multiSchedulingService = new MultiSchedulingService();
            _logger = logger ?? new FileProcessorLogger();

            _cancellationTokenSource = new CancellationTokenSource();
            ValidateConfig();

            if (_config.EnableManualExecution)
            {
                StartManualExecutionListener();
            }
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

            // Determinar qué horarios usar
            var schedulesToUse = GetSchedulesToUse();

            if (schedulesToUse.Count > 1)
            {
                _logger.LogInfo($"Iniciando con {schedulesToUse.Count} horarios programados");
                _multiSchedulingService.ScheduleMultipleTimes(schedulesToUse, () => ProcessFilesForSchedule());
            }
            else if (schedulesToUse.Count == 1)
            {
                _logger.LogInfo("Iniciando con horario único");
                _multiSchedulingService.ScheduleDaily(schedulesToUse[0], () => ProcessFilesForSchedule());
            }

            var message = $"Servicio iniciado. Próxima ejecución: {NextScheduledExecution:yyyy-MM-dd HH:mm:ss}";
            _logger.LogInfo(message);
            OnProcessingStarted?.Invoke(this, new ProcessingEventArgs { Message = message });
        }

        private List<TimeSpan> GetSchedulesToUse()
        {
            if (_config.ScheduledTimes != null && _config.ScheduledTimes.Any())
            {
                return _config.ScheduledTimes.OrderBy(t => t).ToList();
            }
            return new List<TimeSpan> { _config.ScheduledTime };
        }

        public void Stop()
        {
            _multiSchedulingService?.Stop();
            _cancellationTokenSource?.Cancel();

            var message = "Servicio detenido.";
            _logger.LogInfo(message);
            OnProcessingStarted?.Invoke(this, new ProcessingEventArgs { Message = message });
        }

        /// <summary>
        /// Ejecuta el procesamiento manualmente (por señal)
        /// </summary>
        public void TriggerManualExecution()
        {
            if (!_config.EnableManualExecution)
            {
                _logger.LogWarning("Ejecución manual no está habilitada en la configuración");
                return;
            }

            _logger.LogInfo("🔔 Señal de ejecución manual recibida");
            _manualTrigger.Set();

            OnManualExecutionTriggered?.Invoke(this, new ProcessingEventArgs
            {
                Message = "Ejecución manual iniciada",
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Ejecuta el procesamiento de forma asíncrona y espera el resultado
        /// </summary>
        public async Task<ProcessingResult> ExecuteManuallyAsync()
        {
            if (!_config.EnableManualExecution)
            {
                _logger.LogWarning("Ejecución manual no está habilitada en la configuración");
                return new ProcessingResult
                {
                    Success = false,
                    ErrorMessage = "Ejecución manual no habilitada",
                    ProcessedAt = DateTime.Now
                };
            }

            _logger.LogInfo("🔔 Ejecutando procesamiento manual directamente...");
            return await ProcessFiles(isManualExecution: true);
        }

        private void StartManualExecutionListener()
        {
            _manualExecutionTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        _manualTrigger.Wait(_cancellationTokenSource.Token);
                        _manualTrigger.Reset();

                        _logger.LogInfo("Procesando señal de ejecución manual...");
                        await ProcessFiles(isManualExecution: true);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error en listener de ejecución manual: {ex.Message}", ex);
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public async Task<List<FileInfo>> SearchFilesNow()
        {
            _logger.LogInfo("Buscando archivos...");
            var expandedPaths = ExpandSearchPaths();

            // Temporalmente modificar config con rutas expandidas
            var originalPaths = _config.SearchPaths;
            try
            {
                _config.SearchPaths = expandedPaths;
                return await _fileSearchService.FindFilesAsync(_config);
            }
            finally
            {
                _config.SearchPaths = originalPaths;
            }
        }

        public async Task<ProcessingResult> ProcessNow()
        {
            _logger.LogInfo("Iniciando procesamiento inmediato...");
            return await ProcessFiles(isManualExecution: false);
        }

        private async Task<ProcessingResult> ProcessFilesForSchedule()
        {
            var currentSchedule = GetCurrentScheduleTime();
            _logger.LogInfo($"⏰ Ejecutando procesamiento programado para horario: {currentSchedule:hh\\:mm}");

            if (ShouldSkipThisSchedule(currentSchedule))
            {
                var skipMessage = $"Ya se ejecutó en el horario {currentSchedule:hh\\:mm} hoy.";
                _logger.LogInfo(skipMessage);
                return new ProcessingResult { Success = true, ErrorMessage = skipMessage };
            }

            var result = await ProcessFiles(isManualExecution: false);

            if (result.Success)
            {
                _lastExecutionBySchedule[currentSchedule] = DateTime.Now;
            }

            return result;
        }

        private async Task<ProcessingResult> ProcessFiles(bool isManualExecution = false)
        {
            var startTime = DateTime.Now;
            var executionType = isManualExecution ? "Manual" : "Programada";

            try
            {
                await _manualExecutionSemaphore.WaitAsync();

                CheckAndResetDailyTracking();

                _logger.LogInfo($"🔍 Buscando archivos... (Ejecución {executionType})");

                // Expandir rutas con soporte de fecha
                var expandedPaths = ExpandSearchPaths();
                var originalPaths = _config.SearchPaths;

                List<FileInfo> foundFiles;
                try
                {
                    _config.SearchPaths = expandedPaths;
                    foundFiles = await _fileSearchService.FindFilesAsync(_config);
                }
                finally
                {
                    _config.SearchPaths = originalPaths;
                }

                if (!foundFiles.Any())
                {
                    var message = "No se encontraron archivos que coincidan con los criterios especificados.";
                    _logger.LogWarning(message);

                    OnFilesNotFound?.Invoke(this, new ProcessingEventArgs { Message = message });

                    if (_config.SearchUntilFound && !isManualExecution)
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

                // Determinar qué archivos procesar
                var filesToProcess = DetermineFilesToProcess(foundFiles, isManualExecution);

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
                result.Statistics["ExecutionType"] = executionType;

                var successMessage = $"✅ Procesamiento {executionType.ToLower()} completado. Archivos: {filesToProcess.Count}";
                _logger.LogInfo(successMessage);
                OnProcessingCompleted?.Invoke(this, new ProcessingEventArgs { Message = successMessage, Result = result });

                return result;
            }
            catch (Exception ex)
            {
                return HandleProcessingError(ex, startTime, executionType);
            }
            finally
            {
                _manualExecutionSemaphore.Release();
            }
        }

        private List<string> ExpandSearchPaths()
        {
            var expandedPaths = new List<string>();

            foreach (var path in _config.SearchPaths)
            {
                if (_config.EnableDateBasedSearch && _config.DateFolderFormat.HasValue)
                {
                    // Buscar carpetas con fechas
                    var datePaths = DateBasedPathResolver.ResolveDateBasedPaths(
                        path,
                        _config.DateFolderFormat.Value,
                        _config.TargetSearchDate);

                    if (datePaths.Any())
                    {
                        _logger.LogInfo($"📅 Encontradas {datePaths.Count} carpetas con fecha en: {path}");
                        expandedPaths.AddRange(datePaths);
                    }
                    else
                    {
                        _logger.LogWarning($"⚠️ No se encontraron carpetas con formato de fecha en: {path}");
                    }
                }
                else
                {
                    // Ruta normal
                    if (Directory.Exists(path))
                    {
                        expandedPaths.Add(path);
                    }
                }
            }

            // También soportar tokens de fecha en las rutas
            var pathsWithTokens = DateBasedPathResolver.ExpandPathsWithDateTokens(
                _config.SearchPaths,
                _config.TargetSearchDate);

            expandedPaths.AddRange(pathsWithTokens);

            return expandedPaths.Distinct().ToList();
        }

        private TimeSpan GetCurrentScheduleTime()
        {
            var now = DateTime.Now.TimeOfDay;
            var schedulesToUse = GetSchedulesToUse();

            return schedulesToUse
                .OrderBy(t => Math.Abs((t - now).TotalMinutes))
                .First();
        }

        private bool ShouldSkipThisSchedule(TimeSpan scheduleTime)
        {
            if (_config.ProcessOnAllSchedules)
                return false;

            if (_config.ProcessOncePerDay)
            {
                return _lastExecutionBySchedule.Any(kvp => kvp.Value.Date == DateTime.Now.Date);
            }

            return _lastExecutionBySchedule.TryGetValue(scheduleTime, out var lastExecution) &&
                   lastExecution.Date == DateTime.Now.Date;
        }

        private List<FileInfo> DetermineFilesToProcess(List<FileInfo> foundFiles, bool isManualExecution)
        {
            // Si es ejecución manual y se permite múltiples ejecuciones, procesar todo
            if (isManualExecution && _config.AllowMultipleManualExecutions)
            {
                return foundFiles;
            }

            if (_config.ProcessOnAllSchedules)
            {
                return foundFiles;
            }

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
                _logger.LogInfo("🗓️ Nuevo día detectado. Reiniciando tracking.");
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

        private ProcessingResult HandleProcessingError(Exception ex, DateTime startTime, string executionType = "Unknown")
        {
            var result = new ProcessingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.Now,
                ProcessingDuration = DateTime.Now - startTime
            };

            result.Statistics["ExecutionType"] = executionType;

            var errorMessage = $"❌ Error durante procesamiento {executionType.ToLower()}: {ex.Message}";
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

                _multiSchedulingService.ScheduleRetry(_config.RetryInterval, () => ProcessFiles());

                var retryMessage = $"🔄 Reintento programado en {_config.RetryInterval.TotalMinutes} minutos.";
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
                ["NextScheduledTimes"] = GetSchedulesToUse().Select(t => t.ToString(@"hh\:mm")).ToList(),
                ["ProcessOncePerDay"] = _config.ProcessOncePerDay,
                ["ProcessOnAllSchedules"] = _config.ProcessOnAllSchedules,
                ["ManualExecutionEnabled"] = _config.EnableManualExecution,
                ["CurrentDay"] = _currentDay
            };
        }

        public void Dispose()
        {
            Stop();
            _multiSchedulingService?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _manualExecutionSemaphore?.Dispose();
            _manualTrigger?.Dispose();
            _logger.LogInfo("Recursos liberados.");
        }
    }
}
