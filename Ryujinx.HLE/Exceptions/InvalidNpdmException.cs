using System;

namespace Ryujinx.HLE.Exceptions
{
    public class InvalidNpdmException : Exception
    {
        public InvalidNpdmException(string exMsg) : base(exMsg) { }
    }
}
