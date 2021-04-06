using System;

namespace Ryujinx.HLE.HOS.Services
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    class CommandTIpcAttribute : Attribute
    {
        public readonly int Id;

        public CommandTIpcAttribute(int id) => Id = id;
    }
}
