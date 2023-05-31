using System;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    sealed class PointerSizedAttribute : Attribute
    {
    }
}
