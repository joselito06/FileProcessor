using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Utils
{
    public static class FileProcessorHelper
    {
        public static bool IsFileInUse(string filePath)
        {
            try
            {
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                return false;
            }
            catch (IOException)
            {
                return true;
            }
        }

        public static async Task<bool> IsFileInUseAsync(string filePath)
        {
            return await Task.Run(() => IsFileInUse(filePath));
        }

        public static bool WaitForFileAccess(string filePath, TimeSpan timeout)
        {
            var endTime = DateTime.Now + timeout;

            while (DateTime.Now < endTime)
            {
                if (!IsFileInUse(filePath))
                    return true;

                Thread.Sleep(1000); // Esperar 1 segundo
            }

            return false;
        }

        public static async Task<bool> WaitForFileAccessAsync(string filePath, TimeSpan timeout)
        {
            var endTime = DateTime.Now + timeout;

            while (DateTime.Now < endTime)
            {
                if (!await IsFileInUseAsync(filePath))
                    return true;

                await Task.Delay(1000); // Esperar 1 segundo
            }

            return false;
        }

        public static List<FileInfo> SortByDate(List<FileInfo> files, bool descending = true)
        {
            return descending
                ? files.OrderByDescending(f => f.ModifiedAt).ToList()
                : files.OrderBy(f => f.ModifiedAt).ToList();
        }

        public static List<FileInfo> SortBySize(List<FileInfo> files, bool descending = true)
        {
            return descending
                ? files.OrderByDescending(f => f.SizeBytes).ToList()
                : files.OrderBy(f => f.SizeBytes).ToList();
        }

        public static List<FileInfo> SortByName(List<FileInfo> files, bool descending = false)
        {
            return descending
                ? files.OrderByDescending(f => f.FileName).ToList()
                : files.OrderBy(f => f.FileName).ToList();
        }

        public static Dictionary<string, List<FileInfo>> GroupByExtension(List<FileInfo> files)
        {
            return files.GroupBy(f => f.Extension.ToLowerInvariant())
                       .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<string, List<FileInfo>> GroupByDirectory(List<FileInfo> files)
        {
            return files.GroupBy(f => f.Directory)
                       .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static Dictionary<DateTime, List<FileInfo>> GroupByDate(List<FileInfo> files, bool useModifiedDate = true)
        {
            return files.GroupBy(f => (useModifiedDate ? f.ModifiedAt : f.CreatedAt).Date)
                       .ToDictionary(g => g.Key, g => g.ToList());
        }

        public static List<FileInfo> FilterBySize(List<FileInfo> files, long minBytes = 0, long? maxBytes = null)
        {
            var filtered = files.Where(f => f.SizeBytes >= minBytes);

            if (maxBytes.HasValue)
            {
                filtered = filtered.Where(f => f.SizeBytes <= maxBytes.Value);
            }

            return filtered.ToList();
        }

        public static List<FileInfo> FilterByAge(List<FileInfo> files, TimeSpan? maxAge = null, TimeSpan? minAge = null)
        {
            var now = DateTime.Now;
            var filtered = files.AsEnumerable();

            if (maxAge.HasValue)
            {
                var cutoffDate = now - maxAge.Value;
                filtered = filtered.Where(f => f.ModifiedAt >= cutoffDate);
            }

            if (minAge.HasValue)
            {
                var cutoffDate = now - minAge.Value;
                filtered = filtered.Where(f => f.ModifiedAt <= cutoffDate);
            }

            return filtered.ToList();
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        public static async Task<Dictionary<string, object>> GetFileStatistics(List<FileInfo> files)
        {
            return await Task.Run(() =>
            {
                if (!files.Any())
                {
                    return new Dictionary<string, object>
                    {
                        ["Count"] = 0,
                        ["TotalSize"] = 0,
                        ["AverageSize"] = 0
                    };
                }

                var stats = new Dictionary<string, object>
                {
                    ["Count"] = files.Count,
                    ["TotalSize"] = files.Sum(f => f.SizeBytes),
                    ["TotalSizeFormatted"] = FormatFileSize(files.Sum(f => f.SizeBytes)),
                    ["AverageSize"] = (long)files.Average(f => f.SizeBytes),
                    ["AverageSizeFormatted"] = FormatFileSize((long)files.Average(f => f.SizeBytes)),
                    ["LargestFile"] = files.OrderByDescending(f => f.SizeBytes).First(),
                    ["SmallestFile"] = files.OrderBy(f => f.SizeBytes).First(),
                    ["OldestFile"] = files.OrderBy(f => f.ModifiedAt).First(),
                    ["NewestFile"] = files.OrderByDescending(f => f.ModifiedAt).First(),
                    ["Extensions"] = files.GroupBy(f => f.Extension.ToLowerInvariant())
                                         .ToDictionary(g => g.Key, g => g.Count()),
                    ["Directories"] = files.GroupBy(f => f.Directory)
                                          .ToDictionary(g => g.Key, g => g.Count())
                };

                return stats;
            });
        }
    }
}
