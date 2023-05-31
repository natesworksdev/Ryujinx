using System;

namespace Ryujinx.Horizon.Sdk.Sf
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    sealed class CmifCommandAttribute : Attribute
    {
        public uint CommandId { get; }

        public CmifCommandAttribute(uint commandId)
        {
            CommandId = commandId;
        }
    }
}
