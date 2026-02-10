using ExcelFileProcessor.Core.Delegates;
using FileProcessor.Builders;
using FileProcessor.Factory;
using FileProcessor.Services;
using FileProcessor.Utils;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 FileProcessor - Nuevas Funcionalidades");
        Console.WriteLine("==========================================\n");

        Console.WriteLine("Selecciona el ejemplo a ejecutar:");
        Console.WriteLine("1. Múltiples horarios programados");
        Console.WriteLine("2. Búsqueda de carpetas por fecha");
        Console.WriteLine("3. Ejecución manual por señal/evento");
        Console.WriteLine("4. Combinación de todas las funcionalidades");
        Console.Write("\nOpción: ");

        var option = Console.ReadLine();

        switch (option)
        {
            case "1":
                await MultipleSchedulesExample();
                break;
            case "2":
                await DateBasedSearchExample();
                break;
            case "3":
                await ManualExecutionExample();
                break;
            case "4":
                await CombinedExample();
                break;
            default:
                Console.WriteLine("Opción no válida");
                break;
        }

        Console.WriteLine("\n✅ Programa finalizado. Presiona cualquier tecla para salir...");
        Console.ReadKey();
    }

    /// <summary>
    /// EJEMPLO 1: Múltiples horarios programados
    /// </summary>
    static async Task MultipleSchedulesExample()
    {
        Console.WriteLine("\n📅 EJEMPLO 1: Múltiples Horarios Programados");
        Console.WriteLine("===========================================\n");

        var config = new FileSearchConfigBuilder()
            .AddSearchPath(@"C:\FileUtilityTest\Destination")
            .AddFilePattern("Prueba1.xlsx")
            // Programar múltiples horarios en el día
            .ScheduleAtTimes(
                (21, 20),   // 8:00 AM
                (21, 21),  // 12:00 PM
                (21, 22),  // 4:00 PM
                (21, 23)   // 8:00 PM
            )
            .ProcessOnAllSchedules() // Procesar en TODOS los horarios (no solo una vez)
            .RetryEvery(15)
            .Build();

        ProcessFilesDelegate task = async (foundFiles) =>
        {
            Console.WriteLine($"⏰ Procesando {foundFiles.Count} archivos en horario programado...");
            foreach (var file in foundFiles)
            {
                Console.WriteLine($"   📄 {file.FileName} - {file.ModifiedAt:HH:mm:ss}");
            }
            return $"Procesados {foundFiles.Count} archivos";
        };

        var processor = new AdvancedFileProcessorService(config, task);

        // Configurar eventos para ver cuando se ejecuta
        processor.OnProcessingStarted += (s, e) =>
            Console.WriteLine($"🎬 {e.Message}");

        processor.OnProcessingCompleted += (s, e) =>
        {
            Console.WriteLine($"✅ {e.Message}");
            var stats = e.Result?.Statistics;
            if (stats != null)
            {
                Console.WriteLine($"   ⏱️ Duración: {e.Result.ProcessingDuration.TotalSeconds:F1}s");
                Console.WriteLine($"   📊 Archivos procesados hoy: {stats.GetValueOrDefault("TotalProcessedToday", 0)}");
                Console.WriteLine($"   🔄 Ejecuciones del día: {stats.GetValueOrDefault("ScheduledExecutionsToday", 0)}");
            }
        };

        processor.Start();

        Console.WriteLine("\n📋 Estadísticas actuales:");
        var dailyStats = processor.GetDailyStatistics();
        Console.WriteLine($"   Próximos horarios: {string.Join(", ", (System.Collections.IEnumerable)dailyStats["NextScheduledTimes"])}");
        Console.WriteLine($"   Próxima ejecución: {processor.NextScheduledExecution:yyyy-MM-dd HH:mm:ss}");

        Console.WriteLine("\nPresiona ENTER para detener el servicio...");
        Console.ReadLine();

        processor.Stop();
        processor.Dispose();
    }

    /// <summary>
    /// EJEMPLO 2: Búsqueda de carpetas por fecha
    /// </summary>
    static async Task DateBasedSearchExample()
    {
        Console.WriteLine("\n📅 EJEMPLO 2: Búsqueda de Carpetas por Fecha");
        Console.WriteLine("===========================================\n");

        // Simular estructura de carpetas con fechas
        var baseTestPath = @"C:\FileUtilityTest\DateFolders";

        try
        {
            // Crear directorio base
            Directory.CreateDirectory(baseTestPath);

            // Crear carpetas de ejemplo con formato de fecha
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);
            var tomorrow = today.AddDays(1);

            var todayFolderName = today.ToString("dd-MM-yyyy");
            var yesterdayFolderName = yesterday.ToString("dd-MM-yyyy");
            var tomorrowFolderName = tomorrow.ToString("dd-MM-yyyy");

            var todayFolder = Path.Combine(baseTestPath, todayFolderName);
            var yesterdayFolder = Path.Combine(baseTestPath, yesterdayFolderName);
            var tomorrowFolder = Path.Combine(baseTestPath, tomorrowFolderName);

            Directory.CreateDirectory(todayFolder);
            Directory.CreateDirectory(yesterdayFolder);
            Directory.CreateDirectory(tomorrowFolder);

            Console.WriteLine($"📁 Estructura creada: {baseTestPath}");
            Console.WriteLine($"   ├─ {yesterdayFolderName}\\");
            Console.WriteLine($"   ├─ {todayFolderName}\\ ← HOY");
            Console.WriteLine($"   └─ {tomorrowFolderName}\\");

            // Crear archivos de prueba
            File.WriteAllText(Path.Combine(todayFolder, "reporte_hoy.xlsx"), "Archivo de hoy");
            File.WriteAllText(Path.Combine(yesterdayFolder, "reporte_ayer.xlsx"), "Archivo de ayer");
            Console.WriteLine($"\n📄 Archivos creados en cada carpeta\n");

            // Opción 1: Búsqueda automática con formato de fecha
            Console.WriteLine("🔍 OPCIÓN 1: Búsqueda con formato de fecha automático");
            var config1 = new FileSearchConfigBuilder()
                .AddDateBasedSearchPath(
                    baseTestPath,
                    DateBasedPathResolver.DateFolderFormat.DayMonthYear, // dd-MM-yyyy
                    targetDate: DateTime.Now)
                .AddFilePattern("*.xlsx")
                .ScheduleAt(8, 0)
                .Build();

            // Opción 2: Búsqueda con token de fecha en la ruta
            Console.WriteLine("🔍 OPCIÓN 2: Búsqueda con token de fecha");
            var config2 = new FileSearchConfigBuilder()
                .AddSearchPathWithDateToken($@"{baseTestPath}\{{date:dd-MM-yyyy}}")
                .AddFilePattern("*.xlsx")
                .ScheduleAt(8, 0)
                .Build();

            ProcessFilesDelegate task = async (foundFiles) =>
            {
                Console.WriteLine($"\n✅ Archivos encontrados: {foundFiles.Count}");
                foreach (var file in foundFiles)
                {
                    Console.WriteLine($"   📄 {file.FileName}");
                    Console.WriteLine($"      Ruta: {file.FullPath}");
                    Console.WriteLine($"      Directorio: {file.Directory}");
                }
                return foundFiles.Count;
            };

            Console.WriteLine("\n🧪 Probando búsqueda con formato de fecha...");
            var processor1 = new AdvancedFileProcessorService(config1, task);
            var files1 = await processor1.SearchFilesNow();
            Console.WriteLine($"   ✅ Encontrados {files1.Count} archivo(s) en carpeta de HOY");

            Console.WriteLine("\n🧪 Probando búsqueda con token de fecha...");
            var processor2 = new AdvancedFileProcessorService(config2, task);
            var files2 = await processor2.SearchFilesNow();
            Console.WriteLine($"   ✅ Encontrados {files2.Count} archivo(s) usando token");

            // Probar búsqueda de ayer
            Console.WriteLine("\n🧪 Probando búsqueda de AYER...");
            var configYesterday = new FileSearchConfigBuilder()
                .AddDateBasedSearchPath(
                    baseTestPath,
                    DateBasedPathResolver.DateFolderFormat.DayMonthYear,
                    targetDate: DateTime.Now.AddDays(-1))
                .AddFilePattern("*.xlsx")
                .ScheduleAt(8, 0)
                .Build();

            var processorYesterday = new AdvancedFileProcessorService(configYesterday, task);
            var filesYesterday = await processorYesterday.SearchFilesNow();
            Console.WriteLine($"   ✅ Encontrados {filesYesterday.Count} archivo(s) en carpeta de AYER");

            processor1.Dispose();
            processor2.Dispose();
            processorYesterday.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error en el ejemplo: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
        finally
        {
            // Limpiar
            //try
            //{
            //    if (Directory.Exists(baseTestPath))
            //    {
            //        Directory.Delete(baseTestPath, true);
            //        Console.WriteLine("\n🧹 Carpetas de prueba eliminadas");
            //    }
            //}
            //catch { }
        }
    }

    /// <summary>
    /// EJEMPLO 3: Ejecución manual por señal/evento
    /// </summary>
    static async Task ManualExecutionExample()
    {
        Console.WriteLine("\n🎯 EJEMPLO 3: Ejecución Manual por Señal/Evento");
        Console.WriteLine("=============================================\n");

        var config = new FileSearchConfigBuilder()
            .AddSearchPath(@"C:\FileUtilityTest\Destination")
            .AddFilePattern("*.xlsx")
            .ScheduleAt(8, 0)
            .EnableManualExecution(allowMultiple: true) // CLAVE: Habilitar ejecución manual
            .Build();

        ProcessFilesDelegate task = async (foundFiles) =>
        {
            Console.WriteLine($"🎯 Ejecución manual: Procesando {foundFiles.Count} archivos...");
            foreach (var file in foundFiles)
            {
                Console.WriteLine($"   📄 {file.FileName}");
            }
            await Task.Delay(1000); // Simular procesamiento
            return $"Procesados {foundFiles.Count} archivos manualmente";
        };

        var processor = new AdvancedFileProcessorService(config, task);

        processor.OnManualExecutionTriggered += (s, e) =>
            Console.WriteLine($"🔔 {e.Message} - {e.Timestamp:HH:mm:ss}");

        processor.OnProcessingCompleted += (s, e) =>
        {
            var execType = e.Result?.Statistics.GetValueOrDefault("ExecutionType", "Unknown");
            Console.WriteLine($"✅ Completado (Tipo: {execType}) - {e.Message}");
        };

        processor.Start();

        Console.WriteLine("📋 Opciones disponibles:");
        Console.WriteLine("   1. Presiona 'E' para ejecutar manualmente");
        Console.WriteLine("   2. Presiona 'A' para ejecutar manualmente (async/await)");
        Console.WriteLine("   3. Presiona 'S' para ver estadísticas");
        Console.WriteLine("   4. Presiona 'Q' para salir\n");

        bool running = true;
        while (running)
        {
            var key = Console.ReadKey(true);

            switch (key.KeyChar.ToString().ToUpper())
            {
                case "E":
                    Console.WriteLine("🔔 Enviando señal de ejecución manual (TriggerManualExecution)...");
                    processor.TriggerManualExecution();
                    break;

                case "A":
                    Console.WriteLine("🔔 Ejecutando manualmente con await (ExecuteManuallyAsync)...");
                    var result = await processor.ExecuteManuallyAsync();
                    Console.WriteLine($"   Resultado: {(result.Success ? "✅ Éxito" : "❌ Error")}");
                    if (result.Data != null)
                        Console.WriteLine($"   Datos: {result.Data}");
                    break;

                case "S":
                    var stats = processor.GetDailyStatistics();
                    Console.WriteLine("\n📊 Estadísticas del día:");
                    Console.WriteLine($"   Archivos procesados: {stats["ProcessedFilesToday"]}");
                    Console.WriteLine($"   Ejecuciones programadas: {stats["ScheduledExecutionsToday"]}");
                    Console.WriteLine($"   Ejecución manual habilitada: {stats["ManualExecutionEnabled"]}");
                    Console.WriteLine();
                    break;

                case "Q":
                    running = false;
                    break;
            }
        }

        processor.Stop();
        processor.Dispose();
    }

    /// <summary>
    /// EJEMPLO 4: Combinación de todas las funcionalidades
    /// </summary>
    static async Task CombinedExample()
    {
        Console.WriteLine("\n🎨 EJEMPLO 4: Todas las Funcionalidades Combinadas");
        Console.WriteLine("================================================\n");

        // Crear carpetas de prueba
        var reportsBasePath = @"C:\FileUtilityTest\Reports";
        var backupPath = @"C:\FileUtilityTest\Backup";

        try
        {
            // Crear estructura con fecha
            var today = DateTime.Now;
            var todayFolderName = today.ToString("yyyy-MM-dd");
            var todayReportsPath = Path.Combine(reportsBasePath, todayFolderName);

            Directory.CreateDirectory(todayReportsPath);
            Directory.CreateDirectory(backupPath);

            // Crear archivos de prueba
            File.WriteAllText(Path.Combine(todayReportsPath, "informe.xlsx"), "Informe del día");
            File.WriteAllText(Path.Combine(backupPath, "respaldo.xlsx"), "Archivo de respaldo");

            Console.WriteLine($"📁 Estructura creada:");
            Console.WriteLine($"   ├─ {reportsBasePath}\\");
            Console.WriteLine($"   │  └─ {todayFolderName}\\ (HOY)");
            Console.WriteLine($"   │     └─ informe.xlsx");
            Console.WriteLine($"   └─ {backupPath}\\");
            Console.WriteLine($"      └─ respaldo.xlsx\n");

            var config = new FileSearchConfigBuilder()
                // Búsqueda por fecha
                .AddDateBasedSearchPath(
                    reportsBasePath,
                    DateBasedPathResolver.DateFolderFormat.YearMonthDay,
                    targetDate: DateTime.Now)
                // También buscar en ruta fija
                .AddSearchPath(backupPath)
                .AddFilePattern("*.xlsx")
                // Múltiples horarios
                .ScheduleAtTimes(
                    (9, 0),   // 9:00 AM
                    (14, 0),  // 2:00 PM
                    (18, 0)   // 6:00 PM
                )
                .ProcessOnAllSchedules() // Procesar en todos los horarios
                                         // Ejecución manual
                .EnableManualExecution(allowMultiple: true)
                .RetryEvery(10)
                .OnlyFilesNewerThan(1) // Solo archivos del último día
                .Build();

            ProcessFilesDelegate task = async (foundFiles) =>
            {
                Console.WriteLine($"\n🎨 Procesando {foundFiles.Count} archivos...");
                foreach (var file in foundFiles)
                {
                    Console.WriteLine($"   📄 {file.FileName}");
                    Console.WriteLine($"      📁 {file.Directory}");
                    Console.WriteLine($"      📅 Modificado: {file.ModifiedAt:yyyy-MM-dd HH:mm:ss}");
                }
                return new
                {
                    ProcessedCount = foundFiles.Count,
                    ProcessedAt = DateTime.Now,
                    Files = foundFiles.Select(f => f.FileName).ToList()
                };
            };

            var processor = new AdvancedFileProcessorService(config, task);

            // Eventos completos
            processor.OnProcessingStarted += (s, e) =>
                Console.WriteLine($"🎬 {e.Message}");

            processor.OnFilesFound += (s, e) =>
                Console.WriteLine($"🔍 {e.Message}");

            processor.OnProcessingCompleted += (s, e) =>
            {
                Console.WriteLine($"✅ {e.Message}");
                var stats = e.Result?.Statistics;
                if (stats != null)
                {
                    Console.WriteLine($"   📊 Stats:");
                    foreach (var stat in stats)
                    {
                        if (stat.Value != null && !(stat.Value is System.Collections.IEnumerable && stat.Value is not string))
                        {
                            Console.WriteLine($"      {stat.Key}: {stat.Value}");
                        }
                    }
                }
            };

            processor.OnManualExecutionTriggered += (s, e) =>
                Console.WriteLine($"🔔 Ejecución manual iniciada");

            processor.Start();

            Console.WriteLine("\n📋 Configuración activa:");
            var dailyStats = processor.GetDailyStatistics();
            Console.WriteLine($"   Búsqueda por fecha: Habilitada");
            Console.WriteLine($"   Horarios programados: {string.Join(", ", (System.Collections.IEnumerable)dailyStats["NextScheduledTimes"])}");
            Console.WriteLine($"   Ejecución manual: {dailyStats["ManualExecutionEnabled"]}");
            Console.WriteLine($"   Próxima ejecución: {processor.NextScheduledExecution:yyyy-MM-dd HH:mm:ss}");

            Console.WriteLine("\n   Presiona 'E' para ejecutar manualmente | 'Q' para salir");

            bool running = true;
            while (running)
            {
                var key = Console.ReadKey(true);

                if (key.KeyChar.ToString().ToUpper() == "E")
                {
                    var result = await processor.ExecuteManuallyAsync();
                    Console.WriteLine($"\n   ⚡ Resultado manual: {(result.Success ? "✅" : "❌")} - {result.ProcessedFiles?.Count ?? 0} archivos");
                }
                else if (key.KeyChar.ToString().ToUpper() == "Q")
                {
                    running = false;
                }
            }

            processor.Stop();
            processor.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error en el ejemplo: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
        finally
        {
            //Limpiar carpetas de prueba
            try
            {
                if (Directory.Exists(@"C:\FileUtilityTest\Reports"))
                {
                    Directory.Delete(@"C:\FileUtilityTest\Reports", true);
                }
                if (Directory.Exists(@"C:\FileUtilityTest\Backup"))
                {
                    Directory.Delete(@"C:\FileUtilityTest\Backup", true);
                }
                Console.WriteLine("\n🧹 Carpetas de prueba eliminadas");
            }
            catch { }
        }
    }

    static async Task BasicExample()
    {
        // Configuración usando builder pattern
        var config = new FileSearchConfigBuilder()
            .AddSearchPath(@"C:\FileUtilityTest\Destination")
            //.AddSearchPath(@"C:\Reports")
            .AddFileNames("Prueba1.xlsx")
            //.AddFilePatterns("*.xlsx", "*.xls")
            //.ScheduleAt(20, 08) // 9:00 AM
            //.ScheduleAt(20, 09)
            .ScheduleAtTimes((20,29),(20,30))
            //.RetryEvery(2) // 15 minutos
            //.SearchUntilFound()
            .OnlyFilesNewerThan(7) // Solo archivos de los últimos 7 días
            .MaxFileSizeMB(50) // Máximo 50MB
            //.ExcludePatterns("~$*", "temp_*")
            .Build();

        // Tarea personalizada simple
        ProcessFilesDelegate simpleTask = async (foundFiles) =>
        {
            Console.WriteLine($"📁 Procesando {foundFiles.Count} archivos...");

            foreach (var file in foundFiles)
            {
                Console.WriteLine($"   📄 {file.FileName}");
                Console.WriteLine($"      💾 Tamaño: {FileProcessor.Utils.FileProcessorHelper.FormatFileSize(file.SizeBytes)}");
                Console.WriteLine($"      📅 Modificado: {file.ModifiedAt:yyyy-MM-dd HH:mm}");
                Console.WriteLine($"      📁 Ubicación: {file.Directory}");

                // Aquí el usuario puede usar cualquier librería para procesar Excel:
                // - EPPlus: using var package = new ExcelPackage(new FileInfo(file.FullPath));
                // - NPOI: using var fs = new FileStream(file.FullPath, FileMode.Open);
                // - ClosedXML: using var workbook = new XLWorkbook(file.FullPath);
                // - Python: Process.Start("python", $"process_excel.py \"{file.FullPath}\"");
                // - Su API personalizada, etc.

                // Simular procesamiento
                await Task.Delay(100);
            }

            return $"Procesados {foundFiles.Count} archivos exitosamente";
        };

        // Crear el procesador usando factory
        //var processor = FileProcessorFactory.CreateProcessor(config, simpleTask);
        var processor = new EnhancedFileProcessorService(config, simpleTask);


        // Configurar eventos
        processor.OnFilesFound += (sender, e) =>
            Console.WriteLine($"✅ Encontrados: {e.FoundFiles.Count} archivos");

        processor.OnProcessingCompleted += (sender, e) =>
            Console.WriteLine($"🎉 Completado: {e.Message}");

        processor.OnFilesNotFound += (sender, e) =>
            Console.WriteLine($"⚠️ {e.Message}");

        processor.OnProcessingError += (sender, e) =>
            Console.WriteLine($"❌ Error: {e.Message}");

        
        processor.Start();

        // Ejecutar inmediatamente para demo
        /*Console.WriteLine("🔄 Ejecutando procesamiento inmediato...");
        var result = await processor.ProcessNow();

        if (result.Success)
        {
            Console.WriteLine("✅ Procesamiento exitoso!");
            Console.WriteLine($"⏱️ Duración: {result.ProcessingDuration.TotalSeconds:F1} segundos");

            if (result.Statistics.Any())
            {
                Console.WriteLine("📊 Estadísticas:");
                foreach (var stat in result.Statistics)
                {
                    Console.WriteLine($"   {stat.Key}: {stat.Value}");
                }
            }
        }
        else
        {
            Console.WriteLine($"❌ Error: {result.ErrorMessage}");
        }

        processor.Dispose();*/
    }
}