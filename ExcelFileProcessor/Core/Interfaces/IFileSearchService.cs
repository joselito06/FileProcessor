using ExcelFileProcessor.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Interfaces
{
    public interface IFileSearchService
    {
        List<Models.FileInfo> FindFiles(FileSearchConfig config);
        Task<List<Models.FileInfo>> FindFilesAsync(FileSearchConfig config);
        bool ValidatePath(string path);
        Task<List<string>> FindFilesByPatternAsync(string directory, string pattern);
        Task<Dictionary<string, DateTime>> GetFileTimestampsAsync(List<string> filePaths);
        List<Models.FileInfo> ApplyFilters(List<Models.FileInfo> files, FileSearchConfig config);
    }
}
