using System;

namespace Ryujinx.HLE.Exceptions
{
    public sealed class GuestBrokeExecutionException : Exception
    {
        private const string ExMsg = "The guest program broke execution!";

        public GuestBrokeExecutionException() : base(ExMsg) { }
    }
}