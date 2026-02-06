using ExcelFileProcessor.Core.Delegates;
using FileProcessor.Builders;
using FileProcessor.Factory;
using FileProcessor.Services;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 FileProcessor - Ejemplo Completo");
        Console.WriteLine("===================================\n");

        // Ejemplo básico usando el factory
        await BasicExample();

        Console.WriteLine("\nPresiona cualquier tecla para continuar...");
        Console.ReadKey();
    }

    static async Task BasicExample()
    {
        // Configuración usando builder pattern
        var config = new FileSearchConfigBuilder()
            .AddSearchPath(@"C:\FileUtilityTest\Destination")
            //.AddSearchPath(@"C:\Reports")
            .AddFileNames("Prueba1.xlsx")
            //.AddFilePatterns("*.xlsx", "*.xls")
            .ScheduleAt(00, 26) // 9:00 AM
            .ScheduleAt(00, 27)
            .RetryEvery(2) // 15 minutos
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