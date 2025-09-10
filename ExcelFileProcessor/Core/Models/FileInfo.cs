using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcelFileProcessor.Core.Models
{
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
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
