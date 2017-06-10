using System;
using System.IO;

namespace EssentialCsv
{
    public class InvalidFileFormatException : IOException
    {
        public InvalidFileFormatException() : base() { }
        public InvalidFileFormatException(string message) : base(message) { }
        public InvalidFileFormatException(string message, Exception innerException) : base(message, innerException) { }
    }
}
