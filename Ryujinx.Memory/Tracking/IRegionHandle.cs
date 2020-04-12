using System;

namespace Ryujinx.Memory.Tracking
{
    public interface IRegionHandle : IDisposable
    {
        public bool Dirty { get; }

        public ulong Address { get; }
        public ulong Size { get; }
        public ulong EndAddress { get; }

        public void Reprotect();
        public void RegisterAction(RegionSignal action);
    }
}
