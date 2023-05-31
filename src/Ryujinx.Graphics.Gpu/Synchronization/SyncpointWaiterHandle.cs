using System;

namespace Ryujinx.Graphics.Gpu.Synchronization
{
    public sealed class SyncpointWaiterHandle
    {
        internal uint Threshold;
        internal Action<SyncpointWaiterHandle> Callback;
    }
}
