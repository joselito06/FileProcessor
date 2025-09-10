using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileInfo = ExcelFileProcessor.Core.Models.FileInfo;

namespace ExcelFileProcessor.Core.Delegates
{
    /// <summary>
    /// Delegado para definir la tarea personalizada que procesa los archivos encontrados.
    /// El usuario recibe una lista de FileInfo con las rutas de archivos y puede usar
    /// cualquier librería de su elección para procesarlos.
    /// </summary>
    /// <param name="foundFiles">Lista de archivos encontrados con su información</param>
    /// <returns>Datos procesados que serán devueltos en ProcessingResult.Data</returns>
    public delegate Task<object> ProcessFilesDelegate(List<FileInfo> foundFiles);
}
