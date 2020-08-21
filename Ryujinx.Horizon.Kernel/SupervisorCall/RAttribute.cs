using System;

namespace Ryujinx.Horizon.Kernel.SupervisorCall
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class RAttribute : Attribute
    {
        public readonly int Index;

        public RAttribute(int index)
        {
            Index = index;
        }
    }
}
