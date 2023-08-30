using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Common.SystemInterop
{
    public partial class DisplaySleep
    {
        [Flags]
        enum EXECUTION_STATE : uint
        {
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001,
        }

        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int IOPMAssertionCreateWithName(string assertionType, UInt32 assertionLevel, string assertionName, UInt32 assertionId);

        [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int IOPMAssertionRelease(int assertionId);

        static public void Prevent()
        {
            if (OperatingSystem.IsWindows())
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }
            else if (OperatingSystem.IsMacOS())
            {
                // TODO: Assertion ID needs to be a PTR to an int
                IOPMAssertionCreateWithName("NoDisplaySleepAssertion", 255, "Ryujinx", 0);
            }
        }

        static public void Restore()
        {
            if (OperatingSystem.IsWindows())
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
            else if (OperatingSystem.IsMacOS())
            {
                IOPMAssertionRelease(0);
            }
        }
    }
}
