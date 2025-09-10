using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Utils
{
    public static class PathHelper
    {
        public static bool IsValidPath(string path)
        {
            try
            {
                return !string.IsNullOrWhiteSpace(path) &&
                       Path.IsPathRooted(path) &&
                       !Path.GetInvalidPathChars().Any(path.Contains);
            }
            catch
            {
                return false;
            }
        }

        public static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            return Path.GetFullPath(path.Trim());
        }

        public static List<string> ExpandPaths(List<string> paths)
        {
            var expandedPaths = new List<string>();

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    var normalizedPath = NormalizePath(path);

                    if (Directory.Exists(normalizedPath))
                    {
                        expandedPaths.Add(normalizedPath);
                    }
                    else if (File.Exists(normalizedPath))
                    {
                        expandedPaths.Add(Path.GetDirectoryName(normalizedPath));
                    }
                    else
                    {
                        // Puede ser un patrón, agregar el directorio base
                        var directory = Path.GetDirectoryName(normalizedPath);
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        {
                            expandedPaths.Add(directory);
                        }
                    }
                }
                catch
                {
                    // Ignorar rutas inválidas
                }
            }

            return expandedPaths.Distinct().ToList();
        }

        public static async Task<long> GetDirectorySizeAsync(string directoryPath)
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                try
                {
                    return Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                                   .Sum(file => new System.IO.FileInfo(file).Length);
                }
                catch
                {
                    return 0;
                }
            });
        }

        public static async Task<int> GetFileCountAsync(string directoryPath, string pattern = "*")
        {
            return await Task.Run(() =>
            {
                if (!Directory.Exists(directoryPath))
                    return 0;

                try
                {
                    return Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories).Length;
                }
                catch
                {
                    return 0;
                }
            });
        }

        public static string GetRelativePath(string fromPath, string toPath)
        {
            try
            {
                var fromUri = new Uri(fromPath);
                var toUri = new Uri(toPath);

                if (fromUri.Scheme != toUri.Scheme)
                    return toPath;

                var relativeUri = fromUri.MakeRelativeUri(toUri);
                var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

                if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                {
                    relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                }

                return relativePath;
            }
            catch
            {
                return toPath;
            }
        }

        public static bool IsNetworkPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.StartsWith(@"\\");
        }

        public static bool IsSubPathOf(string basePath, string path)
        {
            try
            {
                var baseUri = new Uri(NormalizePath(basePath));
                var pathUri = new Uri(NormalizePath(path));

                return baseUri.IsBaseOf(pathUri);
            }
            catch
            {
                return false;
            }
        }
    }
}
