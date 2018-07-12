using System.Runtime.InteropServices;

namespace Ryujinx.HLE.OsHle.Services.Nv.NvHostCtrl
{
    struct NvHostCtrlGetConfig
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x41)]
        public string DomainString;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x41)]
        public string ParameterString;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x101)]
        public string ConfigurationString;
    }
}