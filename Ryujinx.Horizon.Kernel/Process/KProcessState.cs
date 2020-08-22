namespace Ryujinx.Horizon.Kernel.Process
{
    enum KProcessState : byte
    {
        Created = 0,
        CreatedAttached = 1,
        Started = 2,
        Crashed = 3,
        Attached = 4,
        Exiting = 5,
        Exited = 6,
        DebugSuspended = 7
    }
}