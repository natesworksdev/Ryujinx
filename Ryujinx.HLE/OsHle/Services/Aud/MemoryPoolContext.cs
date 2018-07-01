namespace Ryujinx.HLE.OsHle.Services.Aud
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
