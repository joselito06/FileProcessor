using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Models
{
    public class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        public bool Equals(FileInfo x, FileInfo y)
        {
            return x?.FullPath?.Equals(y?.FullPath, StringComparison.OrdinalIgnoreCase) == true;
        }

        public int GetHashCode(FileInfo obj)
        {
            return obj?.FullPath?.ToLowerInvariant()?.GetHashCode() ?? 0;
        }
    }
}
