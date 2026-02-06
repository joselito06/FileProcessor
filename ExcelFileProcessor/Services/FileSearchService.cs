using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelFileProcessor.Core.Interfaces;
using ExcelFileProcessor.Core.Models;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Services
{
    public class FileSearchService : IFileSearchService
    {
        public List<FileInfo> FindFiles(FileSearchConfig config)
        {
            var foundFiles = new List<FileInfo>();

            foreach (var searchPath in config.SearchPaths)
            {
                if (!Directory.Exists(searchPath))
                    continue;

                // Buscar por nombres específicos
                foreach (var fileName in config.FileNames)
                {
                    var files = FindFilesByName(searchPath, fileName, config.IncludeSubdirectories);
                    foundFiles.AddRange(files);
                }

                // Buscar por patrones
                foreach (var pattern in config.FilePatterns)
                {
                    var files = FindFilesByPattern(searchPath, pattern, config.IncludeSubdirectories);
                    foundFiles.AddRange(files);
                }
            }

            // Aplicar filtros
            var filteredFiles = ApplyFilters(foundFiles, config);

            return filteredFiles.Distinct(new FileInfoComparer()).ToList();
        }

        public async Task<List<FileInfo>> FindFilesAsync(FileSearchConfig config)
        {
            return await Task.Run(() => FindFiles(config));
        }

        public bool ValidatePath(string path)
        {
            try
            {
                return Directory.Exists(path) || File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> FindFilesByPatternAsync(string directory, string pattern)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directory))
                    return new List<string>();

                return Directory.GetFiles(directory, pattern, SearchOption.AllDirectories).ToList();
            });
        }

        public async Task<Dictionary<string, DateTime>> GetFileTimestampsAsync(List<string> filePaths)
        {
            return await Task.Run(() =>
            {
                var timestamps = new Dictionary<string, DateTime>();

                foreach (var filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        timestamps[filePath] = File.GetLastWriteTime(filePath);
                    }
                }

                return timestamps;
            });
        }

        public List<FileInfo> ApplyFilters(List<FileInfo> files, FileSearchConfig config)
        {
            var filteredFiles = files.AsEnumerable();

            // Filtrar por patrones de exclusión
            if (config.ExcludePatterns.Any())
            {
                filteredFiles = filteredFiles.Where(f =>
                    !config.ExcludePatterns.Any(pattern =>
                        IsPatternMatch(f.FileName, pattern)));
            }

            // Filtrar por tamaño máximo
            if (config.MaxFileSizeBytes.HasValue)
            {
                filteredFiles = filteredFiles.Where(f => f.SizeBytes <= config.MaxFileSizeBytes.Value);
            }

            // Filtrar por edad del archivo
            if (config.FileAge.HasValue)
            {
                var cutoffDate = DateTime.Now - config.FileAge.Value;
                filteredFiles = filteredFiles.Where(f => f.ModifiedAt >= cutoffDate);
            }

            return filteredFiles.ToList();
        }

        private List<FileInfo> FindFilesByName(string searchPath, string fileName, bool includeSubdirectories)
        {
            var files = new List<FileInfo>();
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                var foundFilePaths = Directory.GetFiles(searchPath, fileName, searchOption);
                files.AddRange(foundFilePaths.Select(CreateFileInfo));
            }
            catch (Exception)
            {
                // Ignorar errores de acceso
            }

            return files;
        }

        private List<FileInfo> FindFilesByPattern(string searchPath, string pattern, bool includeSubdirectories)
        {
            var files = new List<FileInfo>();
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                var foundFilePaths = Directory.GetFiles(searchPath, pattern, searchOption);
                files.AddRange(foundFilePaths.Select(CreateFileInfo));
            }
            catch (Exception)
            {
                // Ignorar errores de acceso
            }

            return files;
        }

        private FileInfo CreateFileInfo(string filePath)
        {
            var systemFileInfo = new System.IO.FileInfo(filePath);

            return new FileInfo
            {
                FullPath = systemFileInfo.FullName,
                FileName = systemFileInfo.Name,
                Directory = systemFileInfo.DirectoryName,
                SizeBytes = systemFileInfo.Length,
                CreatedAt = systemFileInfo.CreationTime,
                ModifiedAt = systemFileInfo.LastWriteTime,
                AccessedAt = systemFileInfo.LastAccessTime,
                Extension = systemFileInfo.Extension,
                IsReadOnly = systemFileInfo.IsReadOnly
            };
        }

        private bool IsPatternMatch(string fileName, string pattern)
        {
            // Convertir patrón wildcard a regex
            var regexPattern = "^" + pattern.Replace("*", ".*").Replace("?", ".") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(fileName, regexPattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
    }
}
