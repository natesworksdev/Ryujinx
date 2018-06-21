using System;

namespace Ryujinx.HLE.Gpu
{
    class GpuMacroException : Exception
    {
        public GpuMacroException() : base() { }

        public GpuMacroException(string ExMsg) : base(ExMsg) { }
    }
}