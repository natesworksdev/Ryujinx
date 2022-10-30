using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    public interface ILabelScope : IDisposable
    {
        void InsertLabel(string labelName, ColorF labelColor);
    }

    interface ILabelScopePrivate : ILabelScope
    {
        void BeginLabel(string scopeName, ColorF scopeColor);
        void EndLabel();
    }


    class CommandBufferLabelScope : ILabelScopePrivate
    {
        private CommandBuffer _commandBuffer;

        private readonly ExtDebugUtils _debugUtils;

        public unsafe CommandBufferLabelScope(ExtDebugUtils debugUtils, CommandBuffer commandBuffer, string scopeName, ColorF scopeColor)
        {
            _debugUtils = debugUtils;
            _commandBuffer = commandBuffer;

            BeginLabel(scopeName, scopeColor);
        }

        public unsafe void InsertLabel(string labelName, ColorF labelColor)
        {
            IntPtr pLabelName = Marshal.StringToHGlobalAnsi(labelName);

            DebugUtilsLabelEXT label = CreateLabel(pLabelName, labelColor);
            _debugUtils.CmdInsertDebugUtilsLabel(_commandBuffer, label);

            Marshal.FreeHGlobal(pLabelName);
        }

        public unsafe void BeginLabel(string scopeName, ColorF scopeColor)
        {
            IntPtr pScopeName = Marshal.StringToHGlobalAnsi(scopeName);

            DebugUtilsLabelEXT label = CreateLabel(pScopeName, scopeColor);
            _debugUtils.CmdBeginDebugUtilsLabel(_commandBuffer, label);

            Marshal.FreeHGlobal(pScopeName);
        }

        public unsafe void EndLabel()
        {
            _debugUtils.CmdEndDebugUtilsLabel(_commandBuffer);
        }

        private static unsafe DebugUtilsLabelEXT CreateLabel(IntPtr scopeName, ColorF scopeColor)
        {
            DebugUtilsLabelEXT label = new DebugUtilsLabelEXT
            {
                SType = StructureType.DebugUtilsLabelExt,
                PLabelName = (byte*)scopeName
            };
            label.Color[0] = scopeColor.Red;
            label.Color[1] = scopeColor.Green;
            label.Color[2] = scopeColor.Blue;
            label.Color[3] = scopeColor.Alpha;
            return label;
        }

        public void Dispose()
        {
            EndLabel();
        }
    }
}
