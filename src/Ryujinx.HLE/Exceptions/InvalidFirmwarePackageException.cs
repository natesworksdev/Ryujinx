using System;

namespace Ryujinx.HLE.Exceptions
{
    sealed class InvalidFirmwarePackageException : Exception
    {
        public InvalidFirmwarePackageException(string message) : base(message) { }
    }
}