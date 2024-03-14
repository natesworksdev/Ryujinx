using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Memory;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Applets
{
    public interface IApplet
    {
        event EventHandler AppletStateChanged;

        Result Start(AppletSession normalSession,
                     AppletSession interactiveSession);

        Result GetResult();

        bool DrawTo(RenderingSurfaceInfo surfaceInfo, IVirtualMemoryManager destination, ulong position)
        {
            return false;
        }

        static T ReadStruct<T>(ReadOnlySpan<byte> data) where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(data)[0];
        }
    }
}
