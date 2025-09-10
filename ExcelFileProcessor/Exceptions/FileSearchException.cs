using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Exceptions
{
    public class FileSearchException : Exception
    {
        public string SearchPath { get; }
        public string SearchPattern { get; }

        public FileSearchException(string message) : base(message) { }

        public FileSearchException(string message, Exception innerException) : base(message, innerException) { }

        public FileSearchException(string message, string searchPath, string searchPattern = null) : base(message)
        {
            SearchPath = searchPath;
            SearchPattern = searchPattern;
        }

        public FileSearchException(string message, string searchPath, string searchPattern, Exception innerException)
            : base(message, innerException)
        {
            SearchPath = searchPath;
            SearchPattern = searchPattern;
        }
    }
}
