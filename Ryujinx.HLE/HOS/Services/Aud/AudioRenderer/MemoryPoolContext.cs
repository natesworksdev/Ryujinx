namespace Ryujinx.HLE.HOS.Services.Aud.AudioRenderer
{
    internal class MemoryPoolContext
    {
        public MemoryPoolOut OutStatus;

        public MemoryPoolContext()
        {
            OutStatus.State = MemoryPoolState.Detached;
        }
    }
}
