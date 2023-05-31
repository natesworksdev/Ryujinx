using System;

namespace Ryujinx.HLE.Exceptions
{
    public sealed class InvalidNpdmException : Exception
    {
        public InvalidNpdmException(string message) : base(message) { }
    }
}