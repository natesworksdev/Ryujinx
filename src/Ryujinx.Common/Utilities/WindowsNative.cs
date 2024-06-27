using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ryujinx.Common.Utilities
{
    public static partial class WindowsNative
    {
        [SupportedOSPlatform("windows")]
        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        internal static partial int GetShortPathNameW(string longPath, char[] shortPath, int bufferSize);

        private const int ShortPathBufferLength = 256;

        public static string GetShortPathName(string longPath)
        {
            if (!OperatingSystem.IsWindows())
            {
                return "";
            }

            char[] shortPathBuffer = new char[ShortPathBufferLength];
            int result = GetShortPathNameW(longPath, shortPathBuffer, shortPathBuffer.Length);
            if (result == 0)
            {
                int errCode = Marshal.GetLastWin32Error();
                Logging.Logger.Debug?.Print(Logging.LogClass.Application, $"GetShortPathName failed for {longPath} (0x{errCode:X08})");
                return "";
            }

            return new string(shortPathBuffer[..result]);
        }
    }
}
