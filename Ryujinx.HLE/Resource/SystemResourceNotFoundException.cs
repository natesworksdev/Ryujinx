using System;

namespace Ryujinx.HLE.Resource
{
    public class SystemResourceNotFoundException: Exception
    {

        public SystemResourceNotFoundException(string message)
            : base(message)
        {
        }

    }
}