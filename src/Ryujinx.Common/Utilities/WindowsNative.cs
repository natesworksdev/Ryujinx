using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace Ryujinx.Common.Utilities
{
    public static partial class WindowsNative
    {
        [SupportedOSPlatform("windows")]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int GetShortPathName(string longPath, StringBuilder shortPath, int bufferSize);

        private const int ShortPathBufferLength = 256;

        public static string GetShortPathName(string longPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                return "";
            }

            StringBuilder shortPathBuffer = new StringBuilder(ShortPathBufferLength);
            int result = GetShortPathName(longPath, shortPathBuffer, ShortPathBufferLength);
            if (result == 0)
            {
                int errCode = Marshal.GetLastWin32Error();
                Logging.Logger.Debug?.Print(Logging.LogClass.Application, $"GetShortPathName failed for {longPath} (0x{errCode:X08})");
                return "";
            }

            return shortPathBuffer.ToString();
        }
    }
}
