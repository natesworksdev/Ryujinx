using System;

namespace Ryujinx.Cpu.AppleHv
{
    public class DummyDiskCacheLoadState : IDiskCacheLoadState
    {
        /// <inheritdoc/>
        public event Action<LoadState, int, int> StateChanged;

        /// <inheritdoc/>
        public void Cancel()
        {
        }
    }
}