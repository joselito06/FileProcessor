using ExcelFileProcessor.Core.Delegates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace FileProcessor.Factory
{
    public static class ProcessFileDelegateFactory
    {
        /// <summary>
        /// Crea un delegado simple que solo registra información de archivos
        /// </summary>
        public static ProcessFilesDelegate CreateSimpleLogger()
        {
            return async (foundFiles) =>
            {
                var results = new List<object>();

                foreach (var file in foundFiles)
                {
                    var info = new
                    {
                        FileName = file.FileName,
                        Size = Utils.FileProcessorHelper.FormatFileSize(file.SizeBytes),
                        ModifiedAt = file.ModifiedAt,
                        Directory = file.Directory,
                        ProcessedAt = DateTime.Now
                    };

                    results.Add(info);
                    Console.WriteLine($"📄 {file.FileName} ({info.Size}) - {file.ModifiedAt:yyyy-MM-dd HH:mm}");
                }

                return results;
            };
        }

        /// <summary>
        /// Crea un delegado que valida archivos antes de procesarlos
        /// </summary>
        public static ProcessFilesDelegate CreateFileValidator()
        {
            return async (foundFiles) =>
            {
                var validationResult = await Utils.ValidationHelper.ValidateFilesAsync(foundFiles);

                Console.WriteLine($"✅ Archivos válidos: {validationResult.ValidFiles}");
                Console.WriteLine($"❌ Archivos inválidos: {validationResult.InvalidFiles}");
                Console.WriteLine($"📊 Tasa de validez: {validationResult.ValidationRate:F1}%");

                if (validationResult.Errors.Any())
                {
                    Console.WriteLine("Errores encontrados:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"   • {error}");
                    }
                }

                return validationResult;
            };
        }

        /// <summary>
        /// Crea un delegado que ejecuta un comando externo para cada archivo
        /// </summary>
        public static ProcessFilesDelegate CreateExternalCommandProcessor(string command, string arguments = "\"{0}\"")
        {
            return async (foundFiles) =>
            {
                var results = new List<object>();

                foreach (var file in foundFiles)
                {
                    try
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = string.Format(arguments, file.FullPath),
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(processInfo);
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var error = await process.StandardError.ReadToEndAsync();
                        await process.WaitForExitAsync();

                        results.Add(new
                        {
                            FileName = file.FileName,
                            ExitCode = process.ExitCode,
                            Output = output,
                            Error = error,
                            Success = process.ExitCode == 0
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            FileName = file.FileName,
                            Success = false,
                            Error = ex.Message
                        });
                    }
                }

                return results;
            };
        }

        /// <summary>
        /// Crea un delegado que procesa archivos en lotes
        /// </summary>
        public static ProcessFilesDelegate CreateBatchProcessor(int batchSize, Func<List<FileInfo>, Task<object>> batchProcessor)
        {
            return async (foundFiles) =>
            {
                var results = new List<object>();
                var totalBatches = (int)Math.Ceiling(foundFiles.Count / (double)batchSize);

                Console.WriteLine($"📦 Procesando {foundFiles.Count} archivos en {totalBatches} lotes de {batchSize}");

                for (int i = 0; i < foundFiles.Count; i += batchSize)
                {
                    var batch = foundFiles.Skip(i).Take(batchSize).ToList();
                    var currentBatch = (i / batchSize) + 1;

                    Console.WriteLine($"🔄 Procesando lote {currentBatch}/{totalBatches}...");

                    try
                    {
                        var batchResult = await batchProcessor(batch);
                        results.Add(new
                        {
                            BatchNumber = currentBatch,
                            FilesInBatch = batch.Count,
                            Result = batchResult,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            BatchNumber = currentBatch,
                            FilesInBatch = batch.Count,
                            Error = ex.Message,
                            Success = false
                        });
                    }

                    // Pausa entre lotes
                    if (currentBatch < totalBatches)
                    {
                        await Task.Delay(1000);
                    }
                }

                return results;
            };
        }

        /// <summary>
        /// Crea un delegado que copia archivos a otra ubicación
        /// </summary>
        public static ProcessFilesDelegate CreateFileCopier(string destinationPath, bool preserveStructure = false)
        {
            return async (foundFiles) =>
            {
                var results = new List<object>();

                Directory.CreateDirectory(destinationPath);

                foreach (var file in foundFiles)
                {
                    try
                    {
                        string destFile;

                        if (preserveStructure)
                        {
                            // Preservar estructura de directorios
                            var relativePath = Path.GetRelativePath(Path.GetPathRoot(file.FullPath), file.FullPath);
                            destFile = Path.Combine(destinationPath, relativePath);
                            Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                        }
                        else
                        {
                            // Copiar solo el archivo
                            destFile = Path.Combine(destinationPath, file.FileName);
                        }

                        await Task.Run(() => File.Copy(file.FullPath, destFile, overwrite: true));

                        results.Add(new
                        {
                            SourceFile = file.FullPath,
                            DestinationFile = destFile,
                            Success = true,
                            CopiedAt = DateTime.Now
                        });

                        Console.WriteLine($"📋 Copiado: {file.FileName} → {destFile}");
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            SourceFile = file.FullPath,
                            Error = ex.Message,
                            Success = false
                        });

                        Console.WriteLine($"❌ Error copiando {file.FileName}: {ex.Message}");
                    }
                }

                return results;
            };
        }

        /// <summary>
        /// Crea un delegado que genera un reporte de archivos encontrados
        /// </summary>
        public static ProcessFilesDelegate CreateReportGenerator(string reportPath = null)
        {
            return async (foundFiles) =>
            {
                var statistics = await Utils.FileProcessorHelper.GetFileStatistics(foundFiles);
                var report = new
                {
                    GeneratedAt = DateTime.Now,
                    Statistics = statistics,
                    Files = foundFiles.Select(f => new
                    {
                        f.FileName,
                        f.FullPath,
                        SizeFormatted = Utils.FileProcessorHelper.FormatFileSize(f.SizeBytes),
                        f.ModifiedAt,
                        f.Extension,
                        f.Directory
                    }).ToList()
                };

                // Guardar reporte si se especifica ruta
                if (!string.IsNullOrEmpty(reportPath))
                {
                    var reportJson = System.Text.Json.JsonSerializer.Serialize(report, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    var reportFile = Path.Combine(reportPath, $"FileReport_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                    Directory.CreateDirectory(reportPath);
                    await File.WriteAllTextAsync(reportFile, reportJson);

                    Console.WriteLine($"📊 Reporte generado: {reportFile}");
                }

                // Mostrar resumen en consola
                Console.WriteLine($"📊 Resumen del reporte:");
                Console.WriteLine($"   📁 Archivos encontrados: {statistics["Count"]}");
                Console.WriteLine($"   💾 Tamaño total: {statistics["TotalSizeFormatted"]}");
                Console.WriteLine($"   📊 Tamaño promedio: {statistics["AverageSizeFormatted"]}");

                return report;
            };
        }

        /// <summary>
        /// Crea un delegado compuesto que ejecuta múltiples procesadores en secuencia
        /// </summary>
        public static ProcessFilesDelegate CreateCompositeProcessor(params ProcessFilesDelegate[] processors)
        {
            return async (foundFiles) =>
            {
                var results = new List<object>();

                for (int i = 0; i < processors.Length; i++)
                {
                    try
                    {
                        Console.WriteLine($"🔄 Ejecutando procesador {i + 1}/{processors.Length}...");
                        var result = await processors[i](foundFiles);
                        results.Add(new
                        {
                            ProcessorIndex = i + 1,
                            Result = result,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new
                        {
                            ProcessorIndex = i + 1,
                            Error = ex.Message,
                            Success = false
                        });
                    }
                }

                return results;
            };
        }  
    }
}
