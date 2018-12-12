using System;

namespace Ryujinx.HLE.HOS.Kernel
{
    public class InvalidSvcException : Exception
    {
        public InvalidSvcException(string message) : base(message) { }
    }
}
