using System;

namespace Ryujinx.HLE.Exceptions
{
    public sealed class TamperCompilationException : Exception
    {
        public TamperCompilationException(string message) : base(message) { }
    }
}