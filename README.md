# 📁 FileProcessor

Una librería liviana y poderosa en .NET para el procesamiento automático de archivos con programación de tareas avanzada. **Sin dependencias de librerías Excel** - tú decides qué usar para leer los archivos.

## 🚀 Características Principales

- ✅ **Procesamiento Automático**: Ejecuta tareas en horarios específicos
- 🕐 **Múltiples Horarios**: Programa ejecuciones en varios momentos del día
- 📅 **Búsqueda por Fecha**: Encuentra carpetas automáticamente por fecha (ej: `05-02-2026/`)
- 🎯 **Ejecución Manual**: Ejecuta procesamiento bajo demanda con señales/eventos
- 🔄 **Reintentos Inteligentes**: Busca archivos hasta encontrarlos con intervalos configurables
- 📁 **Búsqueda Flexible**: Múltiples rutas y patrones de archivos
- 🎯 **Control de Procesamiento**: Evita procesamientos duplicados con opciones configurables
- 🆓 **Sin Dependencias Excel**: Usa cualquier librería que prefieras (EPPlus, NPOI, ClosedXML, Python, etc.)
- 🔧 **Altamente Configurable**: Personaliza todos los aspectos del comportamiento
- 📝 **Eventos en Tiempo Real**: Notificaciones de estado y progreso
- 🪶 **Liviano**: Solo maneja búsqueda, programación y gestión de archivos

## 📦 Instalación

### Requisitos
- .NET Framework 4.7.2+, .NET 6.0, o .NET 8.0
- **Solo dependencias básicas del framework**

### Via NuGet
```bash
dotnet add package FileProcessor.Core
```

### Tú Eliges la Librería Excel (Opcional)
```bash
# Opción 1: EPPlus (Popular)
dotnet add package EPPlus --version 6.2.10

# Opción 2: NPOI (Gratis, compatible con .xls y .xlsx)
dotnet add package NPOI --version 2.6.0

# Opción 3: ClosedXML (Fácil de usar)
dotnet add package ClosedXML --version 0.102.1

# O cualquier otra librería de tu preferencia
```

## 🎯 Uso Básico

### 1. Procesamiento Automático Simple

```csharp
using FileProcessor;
using FileProcessor.Builders;
using FileProcessor.Services;

// Configuración
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddSearchPath(@"\\Server\Reports")
    .AddFilePattern("*.xlsx")
    .AddFilePattern("ventas_*.xls")
    .ScheduleAt(8, 0) // 8:00 AM
    .RetryEvery(30) // 30 minutos
    .SearchUntilFound()
    .Build();

// Tu tarea personalizada - recibes las rutas de archivos
ProcessFilesDelegate processTask = async (foundFiles) =>
{
    var results = new List<string>();
    
    foreach (var file in foundFiles)
    {
        Console.WriteLine($"📄 Archivo encontrado: {file.FullPath}");
        
        // TÚ DECIDES QUÉ LIBRERÍA USAR:
        // - EPPlus, NPOI, ClosedXML
        // - Python scripts
        // - Tu API personalizada
        
        results.Add($"Procesado: {file.FileName}");
    }
    
    return results;
};

// Crear y configurar el servicio
var processor = new AdvancedFileProcessorService(config, processTask);

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

## 🆕 Nuevas Funcionalidades v2.0

### 1️⃣ Múltiples Horarios Programados

Ejecuta procesamiento automático en **varios momentos del día**:

```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddFilePattern("*.xlsx")
    
    // Programar múltiples horarios
    .ScheduleAtTimes(
        (8, 0),   // 8:00 AM
        (12, 0),  // 12:00 PM
        (16, 0),  // 4:00 PM
        (20, 0)   // 8:00 PM
    )
    
    // Opción 1: Procesar en TODOS los horarios
    .ProcessOnAllSchedules()
    
    // Opción 2: Procesar solo UNA vez al día (predeterminado)
    // .ProcessOncePerDay()
    
    .Build();
```

**Resultado:**
```
🕐 Programando 4 horarios diarios:
   📅 08:00 - en 15.3 minutos
   📅 12:00 - en 255.3 minutos
   📅 16:00 - en 495.3 minutos
   📅 20:00 - en 735.3 minutos
⏰ Próxima ejecución: 2026-02-10 08:00:00
```

### 2️⃣ Búsqueda de Carpetas por Fecha

Busca automáticamente en carpetas organizadas por fecha:

```
C:\FileUtility\
├─ 05-02-2026\
├─ 06-02-2026\
├─ 07-02-2026\
└─ 09-02-2026\  ← Busca automáticamente en HOY
   └─ reporte.xlsx
```

**Uso:**
```csharp
var config = new FileSearchConfigBuilder()
    // Búsqueda automática por fecha
    .AddDateBasedSearchPath(
        @"C:\FileUtility",
        DateBasedPathResolver.DateFolderFormat.DayMonthYear, // dd-MM-yyyy
        targetDate: DateTime.Now // null = fecha actual
    )
    
    // O usando tokens de fecha
    .AddSearchPathWithDateToken(@"C:\Backups\{date:yyyy-MM-dd}")
    
    .AddFilePattern("*.xlsx")
    .Build();
```

**Formatos soportados:**
- `DayMonthYear` → `09-02-2026`
- `YearMonthDay` → `2026-02-09`
- `DayMonthYearCompact` → `09022026`
- `YearMonthDayCompact` → `20260209`
- `DayMonthYearUnderscore` → `09_02_2026`
- `YearMonthDayUnderscore` → `2026_02_09`
- `MonthDayYear` → `02-09-2026`

### 3️⃣ Ejecución Manual por Señal/Evento

Ejecuta procesamiento **cuando TÚ quieras**, ideal para:
- FileSystemWatcher (cuando aparece un archivo)
- Botones en UI
- APIs/Webhooks
- Eventos de otros sistemas

```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddFilePattern("*.xlsx")
    .ScheduleAt(8, 0)
    
    // Habilitar ejecución manual
    .EnableManualExecution(allowMultiple: true)
    
    .Build();

var processor = new AdvancedFileProcessorService(config, processTask);
processor.Start();

// Método 1: Fire and Forget (no espera)
processor.TriggerManualExecution();

// Método 2: Async/Await (espera resultado)
var result = await processor.ExecuteManuallyAsync();
Console.WriteLine($"Procesados: {result.ProcessedFiles.Count} archivos");

// Ejemplo con FileSystemWatcher
var watcher = new FileSystemWatcher(@"C:\Incoming");
watcher.Created += (s, e) =>
{
    if (e.Name.EndsWith(".xlsx"))
    {
        Console.WriteLine($"📁 Nuevo archivo: {e.Name}");
        processor.TriggerManualExecution();
    }
};
watcher.EnableRaisingEvents = true;
```

## 🎨 Ejemplo Completo - Todas las Funcionalidades

```csharp
var config = new FileSearchConfigBuilder()
    // 1. Búsqueda por fecha
    .AddDateBasedSearchPath(
        @"C:\Reportes\Diarios",
        DateBasedPathResolver.DateFolderFormat.DayMonthYear)
    .AddSearchPathWithDateToken(@"C:\Backups\{date:yyyy-MM-dd}")
    
    // 2. También buscar en rutas fijas
    .AddSearchPath(@"C:\Shared\Reports")
    
    .AddFilePattern("*.xlsx")
    
    // 3. Múltiples horarios
    .ScheduleAtTimes(
        (9, 0),   // 9:00 AM
        (14, 0),  // 2:00 PM
        (18, 0)   // 6:00 PM
    )
    .ProcessOnAllSchedules()
    
    // 4. Ejecución manual
    .EnableManualExecution(allowMultiple: true)
    
    .RetryEvery(15)
    .OnlyFilesNewerThan(1)
    .Build();

ProcessFilesDelegate task = async (files) =>
{
    Console.WriteLine($"Procesando {files.Count} archivos...");
    // Tu lógica de procesamiento aquí
    return files.Count;
};

var processor = new AdvancedFileProcessorService(config, task);

// Eventos
processor.OnProcessingCompleted += (s, e) =>
{
    var stats = e.Result.Statistics;
    Console.WriteLine($"Tipo: {stats["ExecutionType"]}"); // "Manual" o "Programada"
    Console.WriteLine($"Archivos: {stats["FilesProcessedNow"]}");
};

processor.Start();

// Ejecutar manualmente cuando aparece un archivo
var watcher = new FileSystemWatcher(@"C:\Incoming");
watcher.Created += (s, e) => processor.TriggerManualExecution();
watcher.EnableRaisingEvents = true;

Console.WriteLine("✅ 3 horarios programados");
Console.WriteLine("✅ Búsqueda automática por fecha");
Console.WriteLine("✅ Ejecución manual habilitada");
```

## 📋 Configuración con Builder Pattern

```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddSearchPaths(@"C:\Reports", @"\\Server\Files")
    
    .AddFileName("reporte_final.xlsx")
    .AddFileNames("ventas.xlsx", "inventario.xlsx")
    
    .AddFilePattern("*.xlsx")
    .AddFilePatterns("reporte_*.xlsx", "ventas_*.xls")
    
    // Programación
    .ScheduleAt(9, 30)
    .ScheduleAtTimes((8, 0), (12, 0), (16, 0))
    .AddScheduleTime(20, 0)
    
    // Comportamiento de ejecución
    .ProcessOnAllSchedules()     // Procesar en todos los horarios
    .ProcessOncePerDay()          // O solo una vez al día
    
    // Reintentos
    .RetryEvery(TimeSpan.FromMinutes(15))
    .RetryEvery(15) // Shortcut en minutos
    .SearchUntilFound()
    .StopSearchingAt(17, 0)
    
    // Filtros de archivos
    .MaxFileSize(50 * 1024 * 1024) // 50MB
    .MaxFileSizeMB(50)
    .OnlyFilesNewerThan(TimeSpan.FromDays(1))
    .OnlyFilesNewerThan(1) // Shortcut en días
    
    // Patrones de exclusión
    .ExcludePattern("~$*")
    .ExcludePatterns("~$*", "temp_*", "backup_*")
    
    // Opciones
    .IncludeSubdirectories()
    
    // Búsqueda por fecha
    .AddDateBasedSearchPath(
        @"C:\Data",
        DateBasedPathResolver.DateFolderFormat.DayMonthYear,
        targetDate: DateTime.Now)
    .AddSearchPathWithDateToken(@"C:\Data\{date:dd-MM-yyyy}")
    
    // Ejecución manual
    .EnableManualExecution(allowMultiple: true)
    .DisableManualExecution()
    
    .Build();
```

## 📊 Modelos de Datos

### FileSearchConfig
```csharp
public class FileSearchConfig
{
    public List<string> SearchPaths { get; set; }
    public List<string> FileNames { get; set; }
    public List<string> FilePatterns { get; set; }
    public TimeSpan ScheduledTime { get; set; }
    public List<TimeSpan> ScheduledTimes { get; set; }
    public TimeSpan RetryInterval { get; set; }
    public bool SearchUntilFound { get; set; }
    public TimeSpan? StopSearchingAfter { get; set; }
    public bool IncludeSubdirectories { get; set; }
    public List<string> ExcludePatterns { get; set; }
    public long? MaxFileSizeBytes { get; set; }
    public TimeSpan? FileAge { get; set; }
    
    // Nuevos en v2.0
    public bool ProcessOncePerDay { get; set; }
    public bool ProcessOnAllSchedules { get; set; }
    public bool EnableDateBasedSearch { get; set; }
    public DateFolderFormat? DateFolderFormat { get; set; }
    public DateTime? TargetSearchDate { get; set; }
    public bool EnableManualExecution { get; set; }
    public bool AllowMultipleManualExecutions { get; set; }
}
```

### FileInfo (Personalizado)
```csharp
public class FileInfo
{
    public string FullPath { get; set; }
    public string FileName { get; set; }
    public string Directory { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public DateTime AccessedAt { get; set; }
    public string Extension { get; set; }
    public bool IsReadOnly { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}
```

### ProcessingResult
```csharp
public class ProcessingResult
{
    public bool Success { get; set; }
    public List<FileInfo> ProcessedFiles { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
    public object Data { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public Dictionary<string, object> Statistics { get; set; }
}
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

// Obtener estadísticas
var stats = await FileProcessorHelper.GetFileStatistics(foundFiles);
```

## 📊 Eventos Disponibles

```csharp
processor.OnProcessingStarted += (sender, e) =>
    Console.WriteLine($"🎬 Iniciado: {e.Message}");

processor.OnFilesFound += (sender, e) =>
    Console.WriteLine($"🔍 Encontrados: {e.FoundFiles.Count} archivos");

processor.OnProcessingCompleted += (sender, e) =>
{
    Console.WriteLine($"✅ Completado: {e.Message}");
    Console.WriteLine($"⏱️ Duración: {e.Result.ProcessingDuration}");
};

processor.OnProcessingError += (sender, e) =>
    Console.WriteLine($"❌ Error: {e.Message}");

processor.OnFilesNotFound += (sender, e) =>
    Console.WriteLine($"⚠️ No encontrados: {e.Message}");

processor.OnRetryScheduled += (sender, e) =>
    Console.WriteLine($"🔄 Reintento: {e.Message}");

// Nuevo en v2.0
processor.OnManualExecutionTriggered += (sender, e) =>
    Console.WriteLine($"🔔 Ejecución manual: {e.Message}");
```

## 📊 Estadísticas y Monitoreo

```csharp
var stats = processor.GetDailyStatistics();

Console.WriteLine($"Archivos procesados hoy: {stats["ProcessedFilesToday"]}");
Console.WriteLine($"Ejecuciones programadas: {stats["ScheduledExecutionsToday"]}");
Console.WriteLine($"Horarios activos: {string.Join(", ", stats["NextScheduledTimes"])}");
Console.WriteLine($"Ejecución manual: {stats["ManualExecutionEnabled"]}");
Console.WriteLine($"Modo: {stats["ProcessOncePerDay"] ? "Una vez/día" : "Todos los horarios"}");

// Próxima ejecución
Console.WriteLine($"Próxima: {processor.NextScheduledExecution:yyyy-MM-dd HH:mm:ss}");

// Estado del servicio
Console.WriteLine($"Activo: {processor.IsRunning}");
```

## 📚 Casos de Uso Comunes

### 1. Procesamiento de Reportes Diarios con Múltiples Horarios
```csharp
var config = new FileSearchConfigBuilder()
    .AddDateBasedSearchPath(
        @"\\ReportServer\Daily",
        DateBasedPathResolver.DateFolderFormat.YearMonthDay)
    .AddFilePattern("reporte_diario_*.xlsx")
    .ScheduleAtTimes((7, 0), (12, 0), (18, 0)) // Mañana, mediodía, tarde
    .ProcessOnAllSchedules()
    .RetryEvery(10)
    .OnlyFilesNewerThan(1)
    .Build();
```

### 2. Monitoreo de Archivos Críticos con Ejecución Manual
```csharp
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\CriticalData")
    .AddFilePattern("*critical*.xlsx")
    .ScheduleAt(0, 0) // Medianoche
    .EnableManualExecution(allowMultiple: true)
    .SearchUntilFound()
    .Build();

var processor = new AdvancedFileProcessorService(config, criticalTask);
processor.Start();

// FileSystemWatcher para archivos críticos
var watcher = new FileSystemWatcher(@"C:\CriticalData");
watcher.Created += (s, e) => processor.TriggerManualExecution();
watcher.EnableRaisingEvents = true;
```

### 3. Procesamiento por Lotes con Carpetas por Fecha
```csharp
var config = new FileSearchConfigBuilder()
    .AddDateBasedSearchPath(
        @"C:\BatchProcessing",
        DateBasedPathResolver.DateFolderFormat.DayMonthYear)
    .AddFilePatterns("*.xlsx", "*.xls", "*.csv")
    .ScheduleAt(23, 0) // 11:00 PM
    .MaxFileSizeMB(100)
    .IncludeSubdirectories()
    .ExcludePatterns("~$*", "temp_*", "backup_*")
    .Build();
```

## 🚦 Manejo de Errores

```csharp
try
{
    var result = await processor.ProcessNow();
    
    if (result.Success)
    {
        Console.WriteLine($"✅ Éxito: {result.ProcessedFiles.Count} archivos");
    }
    else
    {
        Console.WriteLine($"❌ Error: {result.ErrorMessage}");
    }
}
catch (FileSearchException ex)
{
    logger.LogError($"Error de búsqueda: {ex.SearchPath}", ex);
}
catch (ConfigurationException ex)
{
    logger.LogError($"Error de configuración: {ex.Message}", ex);
}
catch (SchedulingException ex)
{
    logger.LogError($"Error de programación: {ex.Message}", ex);
}
```

## 🔄 Migración desde v1.x

### Antes (v1.x)
```csharp
var config = new FileSearchConfig
{
    SearchPaths = new List<string> { @"C:\Data" },
    FilePatterns = new List<string> { "*.xlsx" },
    ScheduledTime = new TimeSpan(8, 0, 0)
};

var processor = new FileProcessorService(config, task);
```

### Ahora (v2.0)
```csharp
// Opción 1: Mantener compatibilidad
var config = new FileSearchConfigBuilder()
    .AddSearchPath(@"C:\Data")
    .AddFilePattern("*.xlsx")
    .ScheduleAt(8, 0)
    .Build();

// Usar AdvancedFileProcessorService para nuevas características
var processor = new AdvancedFileProcessorService(config, task);

// Opción 2: Aprovechar nuevas funcionalidades
var config = new FileSearchConfigBuilder()
    .AddDateBasedSearchPath(@"C:\Data", DateFolderFormat.DayMonthYear)
    .AddFilePattern("*.xlsx")
    .ScheduleAtTimes((8, 0), (12, 0), (16, 0))
    .ProcessOnAllSchedules()
    .EnableManualExecution()
    .Build();

var processor = new AdvancedFileProcessorService(config, task);
```

## 🎉 Ventajas de este Enfoque

### ✅ **Flexibilidad Total**
- Usa EPPlus, NPOI, ClosedXML, Python, R, o cualquier herramienta
- Cambia de librería sin modificar la lógica de programación
- Integra con APIs, bases de datos, servicios web

### ✅ **Liviano y Sin Conflictos**
- No fuerza dependencias específicas
- Evita conflictos de versiones
- Fácil de integrar en proyectos existentes

### ✅ **Escalable**
- Múltiples horarios de ejecución
- Procesamiento paralelo y por lotes
- Manejo eficiente de memoria

### ✅ **Robusto**
- Reintentos automáticos
- Manejo de archivos en uso
- Validaciones y filtros avanzados

### ✅ **Inteligente**
- Búsqueda automática por fecha
- Ejecución manual bajo demanda
- Control granular de procesamiento

## 📖 Documentación Adicional

- **[NUEVAS_FUNCIONALIDADES.md](NUEVAS_FUNCIONALIDADES.md)** - Guía detallada de las nuevas características v2.0
- Ver carpeta `Examples/` para más ejemplos de uso
- Revisar `Program.cs` en el proyecto de pruebas para ejemplos completos

## 📝 Licencia

MIT License - Ver archivo LICENSE para detalles.

## 🆘 Troubleshooting

### Problemas Comunes

**Error: "El archivo está siendo usado por otro proceso"**
```csharp
if (await FileProcessorHelper.IsFileInUseAsync(filePath))
{
    logger.LogWarning($"Archivo en uso, esperando: {filePath}");
    await FileProcessorHelper.WaitForFileAccessAsync(filePath, TimeSpan.FromMinutes(5));
}
```

**Error: "No se encontraron carpetas con formato de fecha"**
```csharp
// Verificar que el formato coincida con tus carpetas
var config = new FileSearchConfigBuilder()
    .AddDateBasedSearchPath(
        @"C:\Data",
        DateBasedPathResolver.DateFolderFormat.DayMonthYear, // Ajustar según tu formato
        targetDate: DateTime.Now)
    .Build();
```

**Múltiples horarios no se están ejecutando todos**
```csharp
// Asegúrate de usar ProcessOnAllSchedules()
var config = new FileSearchConfigBuilder()
    .ScheduleAtTimes((8, 0), (12, 0), (16, 0))
    .ProcessOnAllSchedules() // ← IMPORTANTE
    .Build();
```

---

## 🎯 ¡Listo para usar!

Esta librería te da la **infraestructura de programación y búsqueda de archivos**, mientras **tú mantienes el control total** sobre cómo procesar los archivos. Es la solución perfecta para equipos que ya tienen preferencias de librerías o necesitan integrar con sistemas existentes.

**¿El resultado?** Una librería liviana, flexible y poderosa que se adapta a TUS necesidades, no al revés.

---

**Versión:** 2.0.0  
**Autor:** Joselito Beriguete  
**Empresa:** JBN Code  
**Repositorio:** https://github.com/joselito06/FileProcessor