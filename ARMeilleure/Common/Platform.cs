using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Common
{
    static class Platform
    {
        public enum System
        {
            Windows,
            Linux,
            OSX,
            FreeBSD,
            Unknown
        }

        // Aside from letting us use switch statements instead, both the IL output and the JIT output is _terrible_
        // if you call the RuntimeInformation methods.
        public static readonly System CurrentSystem = System.Unknown;

        static Platform()
        {
            var platforms = new[]
            {
                // (OSPlatform.FreeBSD, Platform.FreeBSD),
                (OSPlatform.Linux, System.Linux),
                (OSPlatform.OSX, System.OSX),
                (OSPlatform.Windows, System.Windows)
            };

            foreach (var platform in platforms)
            {
                if (RuntimeInformation.IsOSPlatform(platform.Item1))
                {
                    CurrentSystem = platform.Item2;
                    return;
                }
            }

            throw new NotImplementedException("Current OS is not implemented");
        }

        public static bool IsWindows => CurrentSystem == System.Windows;
        public static bool IsLinux => CurrentSystem == System.Linux;
    }
}
