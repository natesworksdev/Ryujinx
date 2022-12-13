using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace ARMeilleure.Native
{
    [SupportedOSPlatform("macos")]
    public static class JitSupportDarwin
    {
        [DllImport("libarmeilleure-jitsupport", EntryPoint = "armeilleure_jit_memcpy")]
        public static extern void Copy(IntPtr dst, IntPtr src, ulong n);
    }
}
