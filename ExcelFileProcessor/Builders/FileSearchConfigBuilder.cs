using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Builders
{
    public class FileSearchConfigBuilder
    {
        private readonly FileSearchConfig _config = new FileSearchConfig();

        public FileSearchConfigBuilder AddSearchPath(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                _config.SearchPaths.Add(path);
            }
            return this;
        }

        public FileSearchConfigBuilder AddSearchPaths(params string[] paths)
        {
            foreach (var path in paths)
            {
                AddSearchPath(path);
            }
            return this;
        }

        public FileSearchConfigBuilder AddFileName(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                _config.FileNames.Add(fileName);
            }
            return this;
        }

        public FileSearchConfigBuilder AddFileNames(params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                AddFileName(fileName);
            }
            return this;
        }

        public FileSearchConfigBuilder AddFilePattern(string pattern)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _config.FilePatterns.Add(pattern);
            }
            return this;
        }

        public FileSearchConfigBuilder AddFilePatterns(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                AddFilePattern(pattern);
            }
            return this;
        }

        public FileSearchConfigBuilder ScheduleAt(int hour, int minute, int second = 0)
        {
            _config.ScheduledTime = new TimeSpan(hour, minute, second);
            _config.ScheduledTimes.Clear(); // Limpiar múltiples horarios si se usa horario único
            return this;
        }

        public FileSearchConfigBuilder ScheduleAt(TimeSpan time)
        {
            _config.ScheduledTime = time;
            _config.ScheduledTimes.Clear();
            return this;
        }

        // NUEVOS: MÚLTIPLES HORARIOS
        public FileSearchConfigBuilder ScheduleAtTimes(params TimeSpan[] times)
        {
            _config.ScheduledTimes.Clear();
            _config.ScheduledTimes.AddRange(times);
            return this;
        }

        public FileSearchConfigBuilder ScheduleAtTimes(params (int hour, int minute)[] times)
        {
            _config.ScheduledTimes.Clear();
            foreach (var (hour, minute) in times)
            {
                _config.ScheduledTimes.Add(new TimeSpan(hour, minute, 0));
            }
            return this;
        }

        public FileSearchConfigBuilder AddScheduleTime(int hour, int minute, int second = 0)
        {
            _config.ScheduledTimes.Add(new TimeSpan(hour, minute, second));
            return this;
        }

        public FileSearchConfigBuilder AddScheduleTime(TimeSpan time)
        {
            _config.ScheduledTimes.Add(time);
            return this;
        }

        // NUEVOS: CONFIGURACIÓN DE COMPORTAMIENTO
        public FileSearchConfigBuilder ProcessOncePerDay(bool oncePerDay = true)
        {
            _config.ProcessOncePerDay = oncePerDay;
            return this;
        }

        public FileSearchConfigBuilder ProcessOnAllSchedules(bool onAllSchedules = true)
        {
            _config.ProcessOnAllSchedules = onAllSchedules;
            _config.ProcessOncePerDay = !onAllSchedules; // Son mutuamente exclusivos
            return this;
        }

        public FileSearchConfigBuilder RetryEvery(TimeSpan interval)
        {
            _config.RetryInterval = interval;
            return this;
        }

        public FileSearchConfigBuilder RetryEvery(int minutes)
        {
            _config.RetryInterval = TimeSpan.FromMinutes(minutes);
            return this;
        }

        public FileSearchConfigBuilder SearchUntilFound(bool enabled = true)
        {
            _config.SearchUntilFound = enabled;
            return this;
        }

        public FileSearchConfigBuilder StopSearchingAt(int hour, int minute, int second = 0)
        {
            _config.StopSearchingAfter = new TimeSpan(hour, minute, second);
            return this;
        }

        public FileSearchConfigBuilder StopSearchingAt(TimeSpan time)
        {
            _config.StopSearchingAfter = time;
            return this;
        }

        public FileSearchConfigBuilder IncludeSubdirectories(bool include = true)
        {
            _config.IncludeSubdirectories = include;
            return this;
        }

        public FileSearchConfigBuilder MaxFileSize(long bytes)
        {
            _config.MaxFileSizeBytes = bytes;
            return this;
        }

        public FileSearchConfigBuilder MaxFileSizeMB(int megabytes)
        {
            _config.MaxFileSizeBytes = megabytes * 1024L * 1024L;
            return this;
        }

        public FileSearchConfigBuilder OnlyFilesNewerThan(TimeSpan age)
        {
            _config.FileAge = age;
            return this;
        }

        public FileSearchConfigBuilder OnlyFilesNewerThan(int days)
        {
            _config.FileAge = TimeSpan.FromDays(days);
            return this;
        }

        public FileSearchConfigBuilder ExcludePattern(string pattern)
        {
            if (!string.IsNullOrWhiteSpace(pattern))
            {
                _config.ExcludePatterns.Add(pattern);
            }
            return this;
        }

        public FileSearchConfigBuilder ExcludePatterns(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                ExcludePattern(pattern);
            }
            return this;
        }

        public FileSearchConfigBuilder ClearExcludePatterns()
        {
            _config.ExcludePatterns.Clear();
            return this;
        }

        public FileSearchConfig Build()
        {
            // Validar configuración antes de construir
            var errors = Utils.ValidationHelper.ValidateFileSearchConfig(_config);

            // Validar configuración de horarios
            if (_config.ScheduledTimes.Any() && _config.ScheduledTimes.Count > 1)
            {
                // Verificar que no haya horarios duplicados
                var duplicates = _config.ScheduledTimes.GroupBy(t => t).Where(g => g.Count() > 1);
                if (duplicates.Any())
                {
                    errors.Add("Se encontraron horarios duplicados en ScheduledTimes");
                }

                // Ordenar los horarios automáticamente
                _config.ScheduledTimes = _config.ScheduledTimes.OrderBy(t => t).ToList();
            }

            if (errors.Any())
            {
                throw new Exceptions.ConfigurationException($"Configuración inválida: {string.Join(", ", errors)}");
            }

            return _config;
        }
    }
}
