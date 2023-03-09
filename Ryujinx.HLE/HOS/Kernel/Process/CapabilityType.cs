namespace Ryujinx.HLE.HOS.Kernel.Process
{
    enum CapabilityType : uint
    {
        CorePriority  = (1u <<  3),
        SyscallMask   = (1u <<  4),
        MapRange      = (1u <<  6),
        MapIoPage     = (1u <<  7),
        MapRegion     = (1u << 10),
        InterruptPair = (1u << 11),
        ProgramType   = (1u << 13),
        KernelVersion = (1u << 14),
        HandleTable   = (1u << 15),
        DebugFlags    = (1u << 16),

        Invalid       = 0u,
        Padding       = ~0u
    }
}