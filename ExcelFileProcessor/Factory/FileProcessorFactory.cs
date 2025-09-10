using ExcelFileProcessor.Core.Delegates;
using ExcelFileProcessor.Core.Interfaces;
using ExcelFileProcessor.Core.Models;
using FileProcessor.Builders;
using FileProcessor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Factory
{
    public static class FileProcessorFactory
    {
        public static IFileProcessor CreateProcessor(FileSearchConfig config, ProcessFilesDelegate processTask)
        {
            return new FileProcessorService(config, processTask);
        }

        public static IFileProcessor CreateProcessor(
            FileSearchConfig config,
            ProcessFilesDelegate processTask,
            IFileProcessorLogger logger)
        {
            return new FileProcessorService(config, processTask, logger: logger);
        }

        public static IFileProcessor CreateProcessorWithAllServices(
            FileSearchConfig config,
            ProcessFilesDelegate processTask,
            string logPath = null,
            string configPath = null)
        {
            var logger = CreateLogger(logPath);
            var configService = CreateConfigurationService(configPath);
            var fileSearchService = CreateFileSearchService();
            var schedulingService = CreateSchedulingService();

            return new FileProcessorService(config, processTask, fileSearchService, schedulingService, logger);
        }

        public static IFileSearchService CreateFileSearchService()
        {
            return new FileSearchService();
        }

        public static ISchedulingService CreateSchedulingService()
        {
            return new SchedulingService();
        }

        public static IFileProcessorLogger CreateLogger(string logPath = null)
        {
            return new FileProcessorLogger(logPath);
        }

        public static IConfigurationService CreateConfigurationService(string configPath = null)
        {
            return new ConfigurationService(configPath);
        }

        // Factory methods para configuraciones comunes
        public static FileSearchConfig CreateDailyReportConfig(string reportPath, string filePattern = "*.xlsx")
        {
            return new FileSearchConfigBuilder()
                .AddSearchPath(reportPath)
                .AddFilePattern(filePattern)
                .ScheduleAt(8, 0) // 8:00 AM
                .RetryEvery(30) // 30 minutes
                .SearchUntilFound()
                .IncludeSubdirectories()
                .Build();
        }

        public static FileSearchConfig CreateBatchProcessingConfig(string batchPath, int maxFileSizeMB = 100)
        {
            return new FileSearchConfigBuilder()
                .AddSearchPath(batchPath)
                .AddFilePatterns("*.xlsx", "*.xls", "*.csv")
                .ScheduleAt(23, 0) // 11:00 PM
                .RetryEvery(60) // 1 hour
                .MaxFileSizeMB(maxFileSizeMB)
                .IncludeSubdirectories()
                .ExcludePatterns("~$*", "temp_*", "backup_*")
                .Build();
        }

        public static FileSearchConfig CreateRealTimeMonitorConfig(string watchPath)
        {
            return new FileSearchConfigBuilder()
                .AddSearchPath(watchPath)
                .AddFilePatterns("*.xlsx", "*.xls")
                .RetryEvery(1) // 1 minute
                .SearchUntilFound()
                .OnlyFilesNewerThan(0) // Solo archivos muy recientes
                .Build();
        }
    }
}
