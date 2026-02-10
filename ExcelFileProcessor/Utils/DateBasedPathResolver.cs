using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileProcessor.Utils
{
    /// <summary>
    /// Resuelve rutas dinámicas basadas en fechas
    /// </summary>
    public static class DateBasedPathResolver
    {
        /// <summary>
        /// Formatos de fecha soportados para nombres de carpetas
        /// </summary>
        public enum DateFolderFormat
        {
            /// <summary>dd-MM-yyyy (ejemplo: 05-02-2026)</summary>
            DayMonthYear,
            /// <summary>yyyy-MM-dd (ejemplo: 2026-02-05)</summary>
            YearMonthDay,
            /// <summary>MM-dd-yyyy (ejemplo: 02-05-2026)</summary>
            MonthDayYear,
            /// <summary>ddMMyyyy (ejemplo: 05022026)</summary>
            DayMonthYearCompact,
            /// <summary>yyyyMMdd (ejemplo: 20260205)</summary>
            YearMonthDayCompact,
            /// <summary>dd_MM_yyyy (ejemplo: 05_02_2026)</summary>
            DayMonthYearUnderscore,
            /// <summary>yyyy_MM_dd (ejemplo: 2026_02_05)</summary>
            YearMonthDayUnderscore
        }

        /// <summary>
        /// Resuelve una ruta base buscando subcarpetas con fechas
        /// </summary>
        /// <param name="basePath">Ruta base donde buscar</param>
        /// <param name="format">Formato de fecha esperado</param>
        /// <param name="targetDate">Fecha a buscar (null = fecha actual)</param>
        /// <returns>Lista de rutas que coinciden con la fecha</returns>
        public static List<string> ResolveDateBasedPaths(
            string basePath,
            DateFolderFormat format,
            DateTime? targetDate = null)
        {
            var date = targetDate ?? DateTime.Now;
            var resolvedPaths = new List<string>();

            if (!Directory.Exists(basePath))
                return resolvedPaths;

            try
            {
                var pattern = GetRegexPattern(format);
                var dateString = GetDateString(date, format);

                var directories = Directory.GetDirectories(basePath);

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);

                    // Verificar coincidencia exacta
                    if (dirName.Equals(dateString, StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedPaths.Add(dir);
                        continue;
                    }

                    // Verificar con regex para extraer fecha
                    var match = Regex.Match(dirName, pattern);
                    if (match.Success)
                    {
                        var extractedDate = ExtractDateFromMatch(match, format);
                        if (extractedDate.HasValue && extractedDate.Value.Date == date.Date)
                        {
                            resolvedPaths.Add(dir);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Retornar lista vacía en caso de error
            }

            return resolvedPaths;
        }

        /// <summary>
        /// Expande rutas con soporte para comodines de fecha
        /// Ejemplo: "C:\Data\{date:dd-MM-yyyy}" se expande a "C:\Data\09-02-2026"
        /// </summary>
        public static List<string> ExpandPathsWithDateTokens(
            List<string> paths,
            DateTime? targetDate = null)
        {
            var date = targetDate ?? DateTime.Now;
            var expandedPaths = new List<string>();

            foreach (var path in paths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                // Buscar tokens de fecha: {date:formato}
                var tokenPattern = @"\{date:([^\}]+)\}";
                var match = Regex.Match(path, tokenPattern);

                if (match.Success)
                {
                    var formatString = match.Groups[1].Value;
                    var dateString = date.ToString(formatString);
                    var expandedPath = path.Replace(match.Value, dateString);

                    if (Directory.Exists(expandedPath))
                    {
                        expandedPaths.Add(expandedPath);
                    }
                }
                else if (Directory.Exists(path))
                {
                    expandedPaths.Add(path);
                }
            }

            return expandedPaths;
        }

        /// <summary>
        /// Busca carpetas con fechas en un rango
        /// </summary>
        public static List<string> FindDateFoldersInRange(
            string basePath,
            DateFolderFormat format,
            DateTime startDate,
            DateTime endDate)
        {
            var foundPaths = new List<string>();

            if (!Directory.Exists(basePath))
                return foundPaths;

            var currentDate = startDate.Date;
            while (currentDate <= endDate.Date)
            {
                var paths = ResolveDateBasedPaths(basePath, format, currentDate);
                foundPaths.AddRange(paths);
                currentDate = currentDate.AddDays(1);
            }

            return foundPaths.Distinct().ToList();
        }

        private static string GetRegexPattern(DateFolderFormat format)
        {
            return format switch
            {
                DateFolderFormat.DayMonthYear => @"(\d{2})-(\d{2})-(\d{4})",
                DateFolderFormat.YearMonthDay => @"(\d{4})-(\d{2})-(\d{2})",
                DateFolderFormat.MonthDayYear => @"(\d{2})-(\d{2})-(\d{4})",
                DateFolderFormat.DayMonthYearCompact => @"(\d{2})(\d{2})(\d{4})",
                DateFolderFormat.YearMonthDayCompact => @"(\d{4})(\d{2})(\d{2})",
                DateFolderFormat.DayMonthYearUnderscore => @"(\d{2})_(\d{2})_(\d{4})",
                DateFolderFormat.YearMonthDayUnderscore => @"(\d{4})_(\d{2})_(\d{2})",
                _ => @"(\d{2})-(\d{2})-(\d{4})"
            };
        }

        private static string GetDateString(DateTime date, DateFolderFormat format)
        {
            return format switch
            {
                DateFolderFormat.DayMonthYear => date.ToString("dd-MM-yyyy"),
                DateFolderFormat.YearMonthDay => date.ToString("yyyy-MM-dd"),
                DateFolderFormat.MonthDayYear => date.ToString("MM-dd-yyyy"),
                DateFolderFormat.DayMonthYearCompact => date.ToString("ddMMyyyy"),
                DateFolderFormat.YearMonthDayCompact => date.ToString("yyyyMMdd"),
                DateFolderFormat.DayMonthYearUnderscore => date.ToString("dd_MM_yyyy"),
                DateFolderFormat.YearMonthDayUnderscore => date.ToString("yyyy_MM_dd"),
                _ => date.ToString("dd-MM-yyyy")
            };
        }

        private static DateTime? ExtractDateFromMatch(Match match, DateFolderFormat format)
        {
            try
            {
                int day, month, year;

                switch (format)
                {
                    case DateFolderFormat.DayMonthYear:
                    case DateFolderFormat.DayMonthYearCompact:
                    case DateFolderFormat.DayMonthYearUnderscore:
                        day = int.Parse(match.Groups[1].Value);
                        month = int.Parse(match.Groups[2].Value);
                        year = int.Parse(match.Groups[3].Value);
                        break;

                    case DateFolderFormat.YearMonthDay:
                    case DateFolderFormat.YearMonthDayCompact:
                    case DateFolderFormat.YearMonthDayUnderscore:
                        year = int.Parse(match.Groups[1].Value);
                        month = int.Parse(match.Groups[2].Value);
                        day = int.Parse(match.Groups[3].Value);
                        break;

                    case DateFolderFormat.MonthDayYear:
                        month = int.Parse(match.Groups[1].Value);
                        day = int.Parse(match.Groups[2].Value);
                        year = int.Parse(match.Groups[3].Value);
                        break;

                    default:
                        return null;
                }

                return new DateTime(year, month, day);
            }
            catch
            {
                return null;
            }
        }
    }
}
