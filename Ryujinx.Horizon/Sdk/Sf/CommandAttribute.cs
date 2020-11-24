using System;

namespace Ryujinx.Horizon.Sdk.Sf
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    class CommandAttribute : Attribute
    {
        public uint CommandId { get; }

        public CommandAttribute(uint commandId)
        {
            CommandId = commandId;
        }
    }
}
