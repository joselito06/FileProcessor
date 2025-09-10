using ExcelFileProcessor.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Services
{
    public class FileProcessorLogger : IFileProcessorLogger
    {
        private readonly string _logPath;

        public FileProcessorLogger(string logPath = null)
        {
            _logPath = logPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(_logPath);
        }

        public void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message);
        }

        public void LogError(string message, Exception exception = null)
        {
            var fullMessage = exception != null ? $"{message} | Exception: {exception}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message);
        }

        private void WriteLog(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] [{level}] {message}";

            // Escribir a consola
            Console.WriteLine(logEntry);

            // Escribir a archivo
            try
            {
                var logFile = Path.Combine(_logPath, $"FileProcessor_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }
            catch
            {
                // Ignorar errores de escritura de log
            }
        }
    }
}
