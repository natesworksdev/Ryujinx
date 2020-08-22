using System;

namespace Ryujinx.Horizon.Kernel.Svc
{
    class InvalidSvcException : Exception
    {
        public InvalidSvcException(string message) : base(message) { }
    }
}
