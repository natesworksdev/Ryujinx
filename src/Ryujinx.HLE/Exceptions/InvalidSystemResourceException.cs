using System;

namespace Ryujinx.HLE.Exceptions
{
    public sealed class InvalidSystemResourceException : Exception
    {
        public InvalidSystemResourceException(string message) : base(message) { }
    }
}