using System.Runtime.InteropServices;

namespace Ryujinx
{
    [StructLayout(LayoutKind.Sequential)]
    public struct HidSharedMemHeader //Size: 0x400
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x400)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection1 //Size: 0x400
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x400)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection2 //Size: 0x400
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x400)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection3 //Size: 0x400
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x400)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection4 //Size: 0x400
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x400)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection5 //Size: 0x200
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection6 //Size: 0x200
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection7 //Size: 0x200
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x200)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection8 //Size: 0x800
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x800)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidControllerSerials //Size: 0x4000
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4000)]
        public byte[] Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HidUnknownSection9 //Size: 0x4600
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x4600)]
        public byte[] Padding;
    }
}
