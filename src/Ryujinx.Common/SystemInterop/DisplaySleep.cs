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
        private static partial int IOPMAssertionCreateWithName(string assertionType, UInt32 assertionLevel, string assertionName, IntPtr assertionId);

        [LibraryImport("/System/Library/Frameworks/IOKit.framework/IOKit", StringMarshalling = StringMarshalling.Utf8)]
        private static partial int IOPMAssertionRelease(IntPtr assertionId);

        private GCHandle assertionIdHandle;

        public void Prevent()
        {
            if (OperatingSystem.IsWindows())
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }
            else if (OperatingSystem.IsMacOS())
            {
                assertionIdHandle = GCHandle.Alloc(0, GCHandleType.Pinned);
                IOPMAssertionCreateWithName("NoDisplaySleepAssertion", 255, "Ryujinx", assertionIdHandle.AddrOfPinnedObject());
            }
        }

        public void Restore()
        {
            if (OperatingSystem.IsWindows())
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
            }
            else if (OperatingSystem.IsMacOS())
            {
                IOPMAssertionRelease(assertionIdHandle.AddrOfPinnedObject());
                assertionIdHandle.Free();
            }
        }
    }
}
