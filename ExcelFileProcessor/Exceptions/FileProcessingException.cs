using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Exceptions
{
    public class FileProcessingException : Exception
    {
        public string FilePath { get; }

        public FileProcessingException(string message) : base(message) { }

        public FileProcessingException(string message, Exception innerException) : base(message, innerException) { }

        public FileProcessingException(string message, string filePath) : base(message)
        {
            FilePath = filePath;
        }

        public FileProcessingException(string message, string filePath, Exception innerException) : base(message, innerException)
        {
            FilePath = filePath;
        }
    }
}
