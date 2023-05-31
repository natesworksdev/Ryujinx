using System;

namespace Ryujinx.HLE.HOS.Kernel.SupervisorCall
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    sealed class SvcImplAttribute : Attribute
    {
    }
}
