using System;

namespace Ryujinx.Horizon.Kernel.SupervisorCall
{
    class InvalidSvcException : Exception
    {
        public InvalidSvcException(string message) : base(message) { }
    }
}
