using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessor.Exceptions
{
    public class SchedulingException : Exception
    {
        public SchedulingException(string message) : base(message) { }

        public SchedulingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
