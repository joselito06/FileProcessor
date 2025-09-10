# 📁 FileProcessor

Una librería liviana en .NET para el procesamiento automático de archivos con programación de tareas. **Sin dependencias de librerías Excel** - tú decides qué usar para leer los archivos.

## 🚀 Características Principales

- ✅ **Procesamiento Automático**: Ejecuta tareas en horarios específicos
- 🔄 **Reintentos Inteligentes**: Busca archivos hasta encontrarlos con intervalos configurables
- 📁 **Búsqueda Flexible**: Múltiples rutas y patrones de archivos
- 🎯 **Una Ejecución por Día**: Evita procesamientos duplicados
- 🆓 **Sin Dependencias Excel**: Usa cualquier librería que prefieras (EPPlus, NPOI, ClosedXML, Python, etc.)
- 🔧 **Altamente Configurable**: Personaliza todos los aspectos del comportamiento
- 📝 **Eventos en Tiempo Real**: Notificaciones de estado y progreso
- # 📊 ExcelFileProcessor

Una librería completa en .NET para el procesamiento automático de archivos Excel con programación de tareas y extracción de datos independiente.

## 🚀 Características Principales

- ✅ **Procesamiento Automático**: Ejecuta tareas en horarios específicos
- 🔄 **Reintentos Inteligentes**: Busca archivos hasta encontrarlos con intervalos configurables
- 📁 **Búsqueda Flexible**: Múltiples rutas y patrones de archivos
- 🎯 **Una Ejecución por Día**: Evita procesamientos duplicados
- 📊 **Extracción Independiente**: Extrae datos sin procesamiento programado
- 🪶 **Liviano**: Solo maneja búsqueda, programación y gestión de archivos

## 📦 Instalación

### Requisitos
- .NET 6.0 o superior
- **Solo dependencias básicas del framework**

### Sin Dependencias Externas
```bash
# La librería FileProcessor no requiere paquetes adicionales
dotnet add package FileProcessor
```

### Tú Eliges la Librería Excel (Opcional)
```bash
# Opción 1: EPPlus (Popular)
dotnet add package EPPlus --version 6.2.10

# Opción 2: NPOI (Gratis, compatible con .xls y .xlsx)
dotnet add package NPOI --version 2.6.0

# Opción 3: ClosedXML (Fácil de usar)
dotnet add package ClosedXML --version 0.102.1

# Opción 4: Usar Python con pandas
dotnet add package Python.Runtime --version 3.0.1

# O cualquier otra librería de tu preferencia
```

### Estructura del Proyecto
```
FileProcessor/
├── Core/
│   ├── Models/           # Configuraciones y resultados
│   ├── Interfaces/       # Contratos de servicios
│   └── Delegates/        # Delegados para tareas personalizadas
├── Services/             # Lógica de búsqueda y programación
├── Utils/                # Utilidades para archivos
├── Exceptions/           # Excepciones personalizadas
└── Examples/             # Ejemplos con diferentes librerías
```

## 🏗️ Arquitectura

### Componentes Principales

1. **FileProcessorService**: Servicio principal de procesamiento programado
2. **FileSearchService**: Búsqueda avanzada de archivos con patrones
3. **SchedulingService**: Programación de tareas diarias y reintentos
4. **FileProcessorHelper**: Utilidades para manejo de archivos

### Flujo de Trabajo
1. **Configuración**: Define rutas, patrones y horarios
2. **Búsqueda**: Encuentra archivos según criterios
3. **Entrega**: Te da las rutas de archivos encontrados
4. **Tu Código**: Usas tu librería preferida para procesarlos
5. **Programación**: Se programa para el siguiente ciclo

## 🎯 Uso Básico

### 1. Procesamiento Automático Simple

```csharp
using FileProcessor;

// Configuración
var config = new FileSearchConfig
{
    SearchPaths = new List<string> { @"C:\Data", @"\\Server\Reports" },
    FilePatterns = new List<string> { "*.xlsx", "ventas_*.xls" },
    ScheduledTime = new TimeSpan(8, 0, 0), // 8:00 AM
    RetryInterval = TimeSpan.FromMinutes(30),
    SearchUntilFound = true
};

// Tu tarea personalizada - recibes las rutas de archivos
ProcessFilesDelegate processTask = async (foundFiles) =>
{
    var results = new List<string>();
    
    foreach (var file in foundFiles)
    {
        Console.WriteLine($"📄 Archivo encontrado: {file.FullPath}");
        
        // TÚ DECIDES QUÉ LIBRERÍA USAR:
        
        // Opción 1: EPPlus
        // using var package = new ExcelPackage(new FileInfo(file.FullPath));
        
        // Opción 2: NPOI
        // using var fs = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read);
        // var workbook = WorkbookFactory.Create(fs);
        
        // Opción 3: ClosedXML  
        // using var workbook = new XLWorkbook(file.FullPath);
        
        // Opción 4: Llamar script Python
        // var pythonResult = await RunPythonScript(file.FullPath);
        
        // Opción 5: Tu API personalizada
        // var apiResult = await MyApiProcessor.Process(file.FullPath);
        
        results.Add($"Procesado: {file.FileName}");
    }
    
    return results;
};

// Crear y configurar el servicio
var processor = new FileProcessorService(config, processTask);

// Eventos
processor.OnFilesFound += (sender, e) =>
    Console.WriteLine($"✅ Encontrados: {e.FoundFiles.Count} archivos");

processor.OnProcessingCompleted += (sender, e) =>
    Console.WriteLine($"🎉 Completado en {e.Result.ProcessingDuration.TotalSeconds:F1}s");

// Iniciar servicio automático
processor.Start();

Console.WriteLine("Servicio iniciado. Presiona ENTER para detener...");
Console.ReadLine();
processor.Stop();
```

### 2. Solo Búsqueda de Archivos (Sin Procesamiento)

```csharp
var config = new FileSearchConfig
{
    SearchPaths = new List<string> { @"C:\Reports" },
    FilePatterns = new List<string> { "*.xlsx" },
    IncludeSubdirectories = true,
    FileAge = TimeSpan.FromDays(7) // Solo archivos recientes
};

// Tarea dummy
ProcessFilesDelegate dummyTask = async (files) => "No procesamos";

var processor = new FileProcessorService(config, dummyTask);

// Solo buscar archivos ahora
var foundFiles = await processor.SearchFilesNow();

Console.WriteLine($"🔍 Archivos encontrados: {foundFiles.Count}");
foreach (var file in foundFiles)
{
    Console.WriteLine($"📄 {file.FileName} - {file.ModifiedAt:yyyy-MM-dd}");
    Console.WriteLine($"   📁 {file.FullPath}");
    Console.WriteLine($"   💾 {file.SizeBytes / 1024} KB");
}
```

### 3. Configuración con Builder Pattern

```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddSearchPath(@"\\Server\Files")
    .AddFilePattern("reporte_*.xlsx")
    .AddFilePattern("ventas_*.xls")
    .ScheduleAt(9, 30)
    .RetryEvery(TimeSpan.FromMinutes(15))
    .SearchUntilFound()
    .StopSearchingAt(17, 0)
    .MaxFileSize(50 * 1024 * 1024) // 50MB
    .OnlyFilesNewerThan(TimeSpan.FromDays(1))
    .ExcludePattern("~$*") // Excluir temporales
    .IncludeSubdirectories()
    .Build();
```

## 📋 Modelos de Datos

### FileSearchConfig
```csharp
public class FileSearchConfig
{
    public List<string> SearchPaths { get; set; }           // Rutas de búsqueda
    public List<string> FileNames { get; set; }             // Nombres específicos
    public List<string> FilePatterns { get; set; }          // Patrones (*.xlsx, etc.)
    public TimeSpan ScheduledTime { get; set; }             // Hora de ejecución
    public TimeSpan RetryInterval { get; set; }             // Intervalo de reintentos
    public bool SearchUntilFound { get; set; }              // Buscar hasta encontrar
    public DateTime? StopSearchingAfter { get; set; }       // Límite de búsqueda
    public bool IncludeSubdirectories { get; set; }         // Incluir subdirectorios
    public List<string> ExcludePatterns { get; set; }       // Patrones a excluir
    public long? MaxFileSizeBytes { get; set; }             // Tamaño máximo
    public TimeSpan? FileAge { get; set; }                  // Edad máxima de archivo
}
```

### FileInfo (Personalizado)
```csharp
public class FileInfo
{
    public string FullPath { get; set; }                    // Ruta completa
    public string FileName { get; set; }                    // Nombre del archivo
    public string Directory { get; set; }                   // Directorio
    public long SizeBytes { get; set; }                     // Tamaño en bytes
    public DateTime CreatedAt { get; set; }                 // Fecha de creación
    public DateTime ModifiedAt { get; set; }                // Fecha de modificación
    public DateTime AccessedAt { get; set; }                // Último acceso
    public string Extension { get; set; }                   // Extensión
    public bool IsReadOnly { get; set; }                    // Solo lectura
    public Dictionary<string, object> Metadata { get; set; } // Metadatos adicionales
}
```

## 🎭 Ejemplos con Diferentes Librerías

### Ejemplo 1: Usando EPPlus
```csharp
ProcessFilesDelegate epplusTask = async (foundFiles) =>
{
    var results = new List<object>();
    
    foreach (var file in foundFiles)
    {
        using var package = new ExcelPackage(new System.IO.FileInfo(file.FullPath));
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        
        if (worksheet?.Dimension != null)
        {
            var data = new List<List<object>>();
            for (int row = 1; row <= worksheet.Dimension.Rows; row++)
            {
                var rowData = new List<object>();
                for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                {
                    rowData.Add(worksheet.Cells[row, col].Value);
                }
                data.Add(rowData);
            }
            results.Add(new { FileName = file.FileName, Data = data });
        }
    }
    
    return results;
};
```

### Ejemplo 2: Usando NPOI
```csharp
ProcessFilesDelegate npoiTask = async (foundFiles) =>
{
    var results = new List<object>();
    
    foreach (var file in foundFiles)
    {
        using var fs = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read);
        var workbook = WorkbookFactory.Create(fs);
        var sheet = workbook.GetSheetAt(0);
        
        var data = new List<List<object>>();
        for (int row = 0; row <= sheet.LastRowNum; row++)
        {
            var sheetRow = sheet.GetRow(row);
            if (sheetRow != null)
            {
                var rowData = new List<object>();
                for (int col = 0; col < sheetRow.LastCellNum; col++)
                {
                    var cell = sheetRow.GetCell(col);
                    rowData.Add(cell?.ToString());
                }
                data.Add(rowData);
            }
        }
        
        results.Add(new { FileName = file.FileName, Data = data });
    }
    
    return results;
};
```

### Ejemplo 3: Usando ClosedXML
```csharp
ProcessFilesDelegate closedXmlTask = async (foundFiles) =>
{
    var results = new List<object>();
    
    foreach (var file in foundFiles)
    {
        using var workbook = new XLWorkbook(file.FullPath);
        var worksheet = workbook.Worksheet(1);
        var range = worksheet.RangeUsed();
        
        if (range != null)
        {
            var data = new List<Dictionary<string, object>>();
            var headers = new List<string>();
            
            // Obtener headers
            for (int col = 1; col <= range.ColumnCount(); col++)
            {
                headers.Add(range.Cell(1, col).GetString());
            }
            
            // Obtener datos
            for (int row = 2; row <= range.RowCount(); row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= range.ColumnCount(); col++)
                {
                    rowData[headers[col - 1]] = range.Cell(row, col).Value;
                }
                data.Add(rowData);
            }
            
            results.Add(new { FileName = file.FileName, Headers = headers, Data = data });
        }
    }
    
    return results;
};
```

### Ejemplo 4: Integración con Python
```csharp
ProcessFilesDelegate pythonTask = async (foundFiles) =>
{
    var results = new List<object>();
    
    foreach (var file in foundFiles)
    {
        // Llamar script Python con pandas
        var startInfo = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = $"process_excel.py \"{file.FullPath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(startInfo);
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        results.Add(new { FileName = file.FileName, PythonOutput = output });
    }
    
    return results;
};

// Script Python (process_excel.py)
/*
import pandas as pd
import sys
import json

file_path = sys.argv[1]
df = pd.read_excel(file_path)

result = {
    "rows": len(df),
    "columns": len(df.columns),
    "data": df.to_dict('records')[:10]  # Primeras 10 filas
}

print(json.dumps(result))
*/
```

### Ejemplo 5: API REST Personalizada
```csharp
ProcessFilesDelegate apiTask = async (foundFiles) =>
{
    var httpClient = new HttpClient();
    var results = new List<object>();
    
    foreach (var file in foundFiles)
    {
        // Subir archivo a tu API personalizada
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(file.FullPath));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", file.FileName);
        
        var response = await httpClient.PostAsync("https://your-api.com/process-excel", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        results.Add(new { FileName = file.FileName, ApiResponse = responseContent });
    }
    
    return results;
};
```

## 🔧 Utilidades Incluidas

### Verificar si un archivo está en uso
```csharp
if (FileProcessorHelper.IsFileInUse(filePath))
{
    Console.WriteLine("Archivo en uso, esperando...");
    await FileProcessorHelper.WaitForFileAccessAsync(filePath, TimeSpan.FromMinutes(5));
}
```

### Ordenar y agrupar archivos
```csharp
// Ordenar por fecha (más recientes primero)
var sortedFiles = FileProcessorHelper.SortByDate(foundFiles, descending: true);

// Agrupar por extensión
var groupedByExt = FileProcessorHelper.GroupByExtension(foundFiles);

// Agrupar por directorio
var groupedByDir = FileProcessorHelper.GroupByDirectory(foundFiles);
```

## 📊 Casos de Uso Comunes

### 1. Procesamiento de Reportes Diarios
```csharp
var config = new FileSearchConfig
{
    SearchPaths = new List<string> { @"\\ReportServer\Daily" },
    FilePatterns = new List<string> { "reporte_diario_*.xlsx" },
    ScheduledTime = new TimeSpan(7, 0, 0),
    RetryInterval = TimeSpan.FromMinutes(10),
    FileAge = TimeSpan.FromHours(24)
};

ProcessFilesDelegate dailyReports = async (files) =>
{
    foreach (var file in files)
    {
        // Usar tu librería preferida para procesar
        var data = await ProcessWithYourLibrary(file.FullPath);
        
        // Enviar a base de datos, API, etc.
        await SendToDashboard(data);
        await SendEmailReport(data);
    }
    
    return $"Procesados {files.Count} reportes diarios";
};
```

### 2. Monitoreo de Archivos Críticos
```csharp
var config = new FileSearchConfig
{
    SearchPaths = new List<string> { @"C:\CriticalData" },
    FilePatterns = new List<string> { "*critical*.xlsx", "*urgent*.xlsx" },
    ScheduledTime = new TimeSpan(0, 0, 0), // Medianoche
    RetryInterval = TimeSpan.FromMinutes(5),
    SearchUntilFound = true
};

ProcessFilesDelegate criticalMonitoring = async (files) =>
{
    var alerts = new List<string>();
    
    foreach (var file in files)
    {
        if ((DateTime.Now - file.ModifiedAt).TotalHours > 24)
        {
            alerts.Add($"⚠️ Archivo crítico antiguo: {file.FileName}");
        }
        
        if (file.SizeBytes == 0)
        {
            alerts.Add($"❌ Archivo crítico vacío: {file.FileName}");
        }
    }
    
    if (alerts.Any())
    {
        await SendCriticalAlert(alerts);
    }
    
    return alerts;
};
```

### 3. Procesamiento por Lotes
```csharp
ProcessFilesDelegate batchProcessor = async (foundFiles) =>
{
    const int batchSize = 5;
    var results = new List<object>();
    
    for (int i = 0; i < foundFiles.Count; i += batchSize)
    {
        var batch = foundFiles.Skip(i).Take(batchSize).ToList();
        
        var batchTasks = batch.Select(async file =>
        {
            // Procesamiento paralelo dentro del lote
            return await ProcessFileWithYourLibrary(file.FullPath);
        });
        
        var batchResults = await Task.WhenAll(batchTasks);
        results.AddRange(batchResults);
        
        // Pausa entre lotes
        await Task.Delay(2000);
    }
    
    return results;
};
```

## 🎉 ¡Ventajas de este Enfoque!

### ✅ **Flexibilidad Total**
- Usa EPPlus, NPOI, ClosedXML, Python, R, o cualquier herramienta
- Cambia de librería sin modificar la lógica de programación
- Integra con APIs, bases de datos, servicios web

### ✅ **Liviano y Sin Conflictos**
- No fuerza dependencias específicas
- Evita conflictos de versiones
- Fácil de integrar en proyectos existentes

### ✅ **Escalable**
- Procesamiento paralelo y por lotes
- Manejo eficiente de memoria
- Soporte para archivos grandes

### ✅ **Robusto**
- Reintentos automáticos
- Manejo de archivos en uso
- Validaciones y filtros avanzados

### ✅ **Fácil de Usar**
- Configuración simple y clara
- Eventos informativos
- Builder pattern para configuraciones complejas

---

## 🎯 ¡Listo para usar!

Esta librería te da la **infraestructura de programación y búsqueda de archivos**, mientras **tú mantienes el control total** sobre cómo procesar los archivos Excel. Es la solución perfecta para equipos que ya tienen preferencias de librerías o necesitan integrar con sistemas existentes.

**¿El resultado?** Una librería liviana, flexible y poderosa que se adapta a TUS necesidades, no al revés.
services.AddScoped<IExcelProcessorLogger, ExcelProcessorLogger>();
services.AddScoped<IConfigurationService, ConfigurationService>();

// Uso en controlador o servicio
public class ReportsController : ControllerBase
{
    private readonly IExcelDataExtractor _extractor;
    private readonly IExcelProcessorLogger _logger;
    
    public ReportsController(IExcelDataExtractor extractor, IExcelProcessorLogger logger)
    {
        _extractor = extractor;
        _logger = logger;
    }
    
    [HttpPost("process")]
    public async Task<IActionResult> ProcessExcelFile(IFormFile file)
    {
        var tempPath = Path.GetTempFileName();
        await file.CopyToAsync(new FileStream(tempPath, FileMode.Create));
        
        try
        {
            var result = await _extractor.ExtractFromFileAsync(tempPath);
            return Ok(result);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
```

## 📝 Logging y Monitoreo

### Configuración de Logging
```csharp
// Logger personalizado con múltiples destinos
var logger = new ExcelProcessorLogger();

// O integración con Microsoft.Extensions.Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFile("logs/excel-processor.log");
});

var processor = ExcelServiceFactory.CreateProcessorWithDependencies(
    config, processTask, logPath: "logs/");

// Eventos de logging
processor.OnProcessingStarted += (s, e) => logger.LogInfo($"Iniciado: {e.Message}");
processor.OnProcessingCompleted += (s, e) => logger.LogInfo($"Completado: {e.Message}");
processor.OnProcessingError += (s, e) => logger.LogError($"Error: {e.Message}", e.Exception);
```

### Métricas y Monitoreo
```csharp
public class ProcessingMetrics
{
    public static void TrackProcessingTime(TimeSpan duration)
    {
        // Integración con Application Insights, Prometheus, etc.
        TelemetryClient.TrackMetric("ProcessingDuration", duration.TotalSeconds);
    }
    
    public static void TrackFilesProcessed(int count)
    {
        TelemetryClient.TrackMetric("FilesProcessed", count);
    }
    
    public static void TrackErrors(string errorType)
    {
        TelemetryClient.TrackEvent("ProcessingError", new Dictionary<string, string>
        {
            ["ErrorType"] = errorType,
            ["Timestamp"] = DateTime.UtcNow.ToString()
        });
    }
}
```

## 🚦 Manejo de Errores

### Excepciones Personalizadas
```csharp
try
{
    var result = await processor.ProcessNow();
}
catch (ExcelProcessingException ex)
{
    logger.LogError($"Error de procesamiento en archivo: {ex.FilePath}", ex);
}
catch (FileNotFoundException ex)
{
    logger.LogWarning($"Archivo no encontrado: {ex.FilePath}");
}
catch (ConfigurationException ex)
{
    logger.LogError($"Error de configuración: {ex.Message}", ex);
}
```

### Validación Robusta
```csharp
var validationErrors = ValidationHelper.ValidateFileSearchConfig(config);
if (validationErrors.Any())
{
    throw new ConfigurationException($"Configuración inválida: {string.Join(", ", validationErrors)}");
}

foreach (var filePath in foundFiles)
{
    if (!ValidationHelper.IsValidExcelFile(filePath, out var error))
    {
        logger.LogWarning($"Archivo inválido {filePath}: {error}");
        continue;
    }
}
```

## ⚡ Optimización y Rendimiento

### Procesamiento Paralelo
```csharp
ProcessFileDataDelegate parallelProcessor = async (packages, paths) =>
{
    var tasks = packages.Select(async (package, index) =>
    {
        var filePath = paths[index];
        return await ProcessSinglePackageAsync(package, filePath);
    });
    
    var results = await Task.WhenAll(tasks);
    return results.SelectMany(r => r).ToList();
};
```

### Procesamiento por Lotes
```csharp
public async Task ProcessLargeDirectory(string directoryPath)
{
    var files = Directory.GetFiles(directoryPath, "*.xlsx");
    var batchSize = 10;
    
    for (int i = 0; i < files.Length; i += batchSize)
    {
        var batch = files.Skip(i).Take(batchSize).ToList();
        
        var results = await extractor.ExtractFromFilesAsync(batch);
        await ProcessBatchResults(results);
        
        // Pausa para evitar sobrecarga
        await Task.Delay(1000);
    }
}
```

### Gestión de Memoria
```csharp
// Configuración para archivos grandes
var config = new ExtractionConfig
{
    MaxRows = 10000,      // Limitar filas por lote
    MaxColumns = 50,      // Limitar columnas
    IncludeEmptyRows = false,
    IncludeEmptyCells = false
};

// Procesamiento streaming para archivos muy grandes
public async IAsyncEnumerable<T> StreamExcelDataAsync<T>(string filePath) where T : class, new()
{
    using var package = new ExcelPackage(new FileInfo(filePath));
    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
    
    if (worksheet?.Dimension != null)
    {
        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            yield return MapRowToObject<T>(worksheet, row);
            
            // Yield control para evitar bloqueo
            if (row % 100 == 0)
                await Task.Yield();
        }
    }
}
```

## 🔒 Seguridad

### Validación de Archivos
```csharp
public static bool IsSafeExcelFile(string filePath)
{
    // Verificar extensión
    var allowedExtensions = new[] { ".xlsx", ".xls" };
    var extension = Path.GetExtension(filePath).ToLower();
    if (!allowedExtensions.Contains(extension))
        return false;
    
    // Verificar tamaño máximo (ej: 50MB)
    var fileInfo = new FileInfo(filePath);
    if (fileInfo.Length > 50 * 1024 * 1024)
        return false;
    
    // Verificar que es realmente un archivo Excel
    try
    {
        using var package = new ExcelPackage(fileInfo);
        return package.Workbook.Worksheets.Count > 0;
    }
    catch
    {
        return false;
    }
}
```

### Sanitización de Datos
```csharp
public static string SanitizeString(string input)
{
    if (string.IsNullOrEmpty(input))
        return string.Empty;
    
    // Remover caracteres peligrosos
    var dangerous = new[] { "<", ">", "&", "\"", "'", "=", "+", "-", "@" };
    var sanitized = input;
    
    foreach (var character in dangerous)
    {
        sanitized = sanitized.Replace(character, "");
    }
    
    return sanitized.Trim();
}
```

## 🧪 Testing

### Unit Tests
```csharp
[Test]
public async Task ExtractFromFile_ValidFile_ReturnsSuccess()
{
    // Arrange
    var extractor = new ExcelDataExtractorService();
    var testFile = CreateTestExcelFile();
    
    // Act
    var result = await extractor.ExtractFromFileAsync(testFile);
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.IsNotNull(result.Worksheets);
    Assert.Greater(result.Worksheets.Count, 0);
}

[Test]
public void ValidateConfig_InvalidPaths_ThrowsException()
{
    // Arrange
    var config = new FileSearchConfig
    {
        SearchPaths = new List<string>(),
        FileNames = new List<string> { "test.xlsx" }
    };
    
    // Act & Assert
    Assert.Throws<ArgumentException>(() => new ExcelFileProcessorService(config, null));
}
```

### Integration Tests
```csharp
[Test]
public async Task EndToEndProcessing_RealFiles_ProcessesSuccessfully()
{
    // Arrange
    var testDirectory = SetupTestDirectory();
    var config = CreateTestConfig(testDirectory);
    var processor = new ExcelFileProcessorService(config, TestProcessingTask);
    
    // Act
    var result = await processor.ProcessNow();
    
    // Assert
    Assert.IsTrue(result.Success);
    Assert.IsNotEmpty(result.ProcessedFiles);
    
    // Cleanup
    CleanupTestDirectory(testDirectory);
}
```

## 📚 Casos de Uso Comunes

### 1. Procesamiento de Nómina
```csharp
var payrollConfig = new FileSearchConfig
{
    SearchPaths = new List<string> { @"\\HR-Server\Payroll" },
    FileNames = new List<string> { "nomina_*.xlsx" },
    ScheduledTime = new TimeSpan(6, 0, 0),
    RetryInterval = TimeSpan.FromMinutes(15)
};

ProcessFileDataDelegate payrollProcessor = async (packages, paths) =>
{
    var employees = new List<Employee>();
    
    foreach (var package in packages)
    {
        employees.AddRange(await ExtractEmployeeData(package));
    }
    
    await ProcessPayroll(employees);
    await GeneratePayrollReports(employees);
    await SendHRNotification(employees.Count);
    
    return employees.Count;
};
```

### 2. Consolidación de Ventas Regionales
```csharp
var salesConfig = new FileSearchConfig
{
    SearchPaths = new List<string> 
    { 
        @"\\Regional1\Sales", 
        @"\\Regional2\Sales", 
        @"\\Regional3\Sales" 
    },
    FileNames = new List<string> { "ventas_diarias.xlsx" },
    ScheduledTime = new TimeSpan(23, 0, 0) // 11:00 PM
};

ProcessFileDataDelegate salesConsolidation = async (packages, paths) =>
{
    var regionalSales = new Dictionary<string, List<Sale>>();
    
    for (int i = 0; i < packages.Count; i++)
    {
        var region = ExtractRegionFromPath(paths[i]);
        var sales = await ExtractSalesData(packages[i]);
        regionalSales[region] = sales;
    }
    
    var consolidatedReport = await GenerateConsolidatedReport(regionalSales);
    await UploadToDataWarehouse(consolidatedReport);
    
    return consolidatedReport;
};
```

### 3. Monitoreo de Inventario
```csharp
var inventoryConfig = new FileSearchConfig
{
    SearchPaths = new List<string> { @"C:\Inventory\Daily" },
    FileNames = new List<string> { "inventario.xlsx", "movimientos.xlsx" },
    ScheduledTime = new TimeSpan(7, 30, 0),
    RetryInterval = TimeSpan.FromMinutes(10),
    StopSearchingAfter = new TimeSpan(9, 0, 0)
};

ProcessFileDataDelegate inventoryMonitoring = async (packages, paths) =>
{
    var alerts = new List<InventoryAlert>();
    
    foreach (var package in packages)
    {
        var inventory = await ExtractInventoryData(package);
        
        // Detectar stock bajo
        var lowStockItems = inventory.Where(i => i.Stock <= i.MinStock);
        alerts.AddRange(lowStockItems.Select(item => new InventoryAlert
        {
            ProductCode = item.Code,
            CurrentStock = item.Stock,
            MinStock = item.MinStock,
            AlertType = "LOW_STOCK"
        }));
        
        // Detectar productos vencidos
        var expiredItems = inventory.Where(i => i.ExpiryDate <= DateTime.Now.AddDays(7));
        alerts.AddRange(expiredItems.Select(item => new InventoryAlert
        {
            ProductCode = item.Code,
            ExpiryDate = item.ExpiryDate,
            AlertType = "EXPIRING_SOON"
        }));
    }
    
    if (alerts.Any())
    {
        await SendInventoryAlerts(alerts);
    }
    
    return alerts;
};
```

## 🔧 Configuración Avanzada

### Archivo de Configuración (appsettings.json)
```json
{
  "ExcelProcessor": {
    "DefaultScheduledTime": "08:00:00",
    "DefaultRetryInterval": "00:30:00",
    "MaxFileSize": 52428800,
    "LogLevel": "Information",
    "LogPath": "logs/excel-processor",
    "EnableMetrics": true,
    "EnableRetries": true,
    "MaxRetryAttempts": 5,
    "SearchPaths": [
      "C:\\Data\\Excel",
      "\\\\FileServer\\Reports"
    ],
    "FilePatterns": [
      "*.xlsx",
      "*.xls"
    ],
    "ExcludePatterns": [
      "~$*",
      "temp_*"
    ]
  }
}
```

### Builder Pattern para Configuración
```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddSearchPath(@"\\Server\Reports")
    .AddFileName("ventas.xlsx")
    .AddFilePattern("reporte_*.xlsx")
    .ScheduleAt(8, 0)
    .RetryEvery(TimeSpan.FromMinutes(30))
    .SearchUntilFound()
    .StopSearchingAt(18, 0)
    .Build();
```

## 📞 Soporte y Contribución

### Reportar Issues
- Usa el template de issue en GitHub
- Incluye logs relevantes
- Proporciona archivos de ejemplo (sin datos sensibles)

### Contribuir
1. Fork el repositorio
2. Crea una rama feature
3. Implementa tests
4. Envía un Pull Request

### Roadmap
- ✅ Procesamiento básico programado
- ✅ Extracción de datos independiente
- 🚧 Soporte para archivos CSV
- 🚧 Integración con Azure Blob Storage
- 📋 Soporte para Google Sheets
- 📋 Dashboard de monitoreo web
- 📋 API REST completa

## 📜 Licencia

MIT License - Ver archivo LICENSE para detalles.

## 🆘 Troubleshooting

### Problemas Comunes

**Error: "El archivo está siendo usado por otro proceso"**
```csharp
// Verificar si el archivo está en uso
if (await ExcelHelper.IsFileInUseAsync(filePath))
{
    logger.LogWarning($"Archivo en uso, reintentando: {filePath}");
    await Task.Delay(5000);
    // Reintentar o programar para más tarde
}
```

**Error: "Memoria insuficiente"**
```csharp
// Procesar archivos en lotes más pequeños
var config = new ExtractionConfig
{
    MaxRows = 1000,
    MaxColumns = 20
};

// O usar streaming para archivos grandes
await foreach (var data in StreamExcelDataAsync<MyClass>(filePath))
{
    await ProcessDataChunk(data);
}
```

**Error: "Formato de archivo no válido"**
```csharp
// Validar archivo antes de procesarlo
if (!ValidationHelper.IsValidExcelFile(filePath, out var error))
{
    logger.LogError($"Archivo inválido {filePath}: {error}");
    continue;
}
```

---

## 🎉 ¡Listo para usar!

Esta librería te proporciona una solución completa y robusta para el procesamiento automatizado de archivos Excel. Con su arquitectura modular y extensible, puedes adaptarla fácilmente a tus necesidades específicas.
