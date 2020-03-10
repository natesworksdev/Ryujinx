using System;

namespace Ryujinx.Graphics.Gpu.Synchronization
{
    public class SyncpointWaiterInformation
    {
        public uint   Threshold;
        public Action Callback;
    }
}
