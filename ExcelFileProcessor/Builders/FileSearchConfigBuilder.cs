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
            return this;
        }

        public FileSearchConfigBuilder ScheduleAt(TimeSpan time)
        {
            _config.ScheduledTime = time;
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
            if (errors.Any())
            {
                throw new Exceptions.ConfigurationException($"Configuración inválida: {string.Join(", ", errors)}");
            }

            return _config;
        }
    }
}
