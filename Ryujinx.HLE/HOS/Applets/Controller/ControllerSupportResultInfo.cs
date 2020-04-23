using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Applets
{
    [StructLayout(LayoutKind.Explicit, Size = 12)]
    unsafe struct ControllerSupportResultInfo
    {
        [FieldOffset(0)] public sbyte PlayerCount;
        [FieldOffset(4)] public uint SelectedId;
#pragma warning disable CS0649
        [FieldOffset(8)] public uint Result;
#pragma warning restore CS0649
    }
}