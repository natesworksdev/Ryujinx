using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Common.System
{
    public class DisplaySleep
    {
        static private Timer _resetTimer;

        static private IntPtr _display;

        [Flags]
        enum EXECUTION_STATE : uint
        {
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [DllImport("libX11", EntryPoint = "XResetScreenSaver")]
        static extern void XResetScreenSaver(IntPtr display);

        [DllImport("libX11", EntryPoint = "XOpenDisplay")]
        static extern IntPtr XOpenDisplay(IntPtr display);

        [DllImport("libX11", EntryPoint = "XCloseDisplay")]
        static extern int XCloseDisplay(IntPtr display);

        static public void Prevent()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _resetTimer = new Timer((object state) =>
                {
                    _display = XOpenDisplay(IntPtr.Zero);
                    XResetScreenSaver(_display);
                    XCloseDisplay(_display);
                }, null, 0, 25000);
            }
        }

        static public void Restore()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);  
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _resetTimer.Dispose();
            }
        }
    }
}
