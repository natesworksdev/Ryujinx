using System;

namespace Ryujinx.HLE.Exceptions
{
    public sealed class TamperExecutionException : Exception
    {
        public TamperExecutionException(string message) : base(message) { }
    }
}