using Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager.Types;

namespace Ryujinx.HLE.HOS.Services.Audio.AudioRendererManager
{
    class MemoryPoolContext
    {
        public MemoryPoolOut OutStatus;

        public MemoryPoolContext()
        {
            OutStatus.State = MemoryPoolState.Detached;
        }
    }
}