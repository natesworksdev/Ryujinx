using Ryujinx.Common.Memory;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ControllerSupportResultInfo
    {
        public sbyte         PlayerCount;
        private Array3<byte> _padding;
        public uint          SelectedId;
        public uint          Result;
    }
}