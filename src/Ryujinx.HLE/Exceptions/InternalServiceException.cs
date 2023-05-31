using System;

namespace Ryujinx.HLE.Exceptions
{
    sealed class InternalServiceException: Exception
    {
        public InternalServiceException(string message) : base(message) { }
    }
}