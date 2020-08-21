namespace Ryujinx.Horizon.Kernel.SupervisorCall
{
    public enum InfoType : uint
    {
        CoreMask = 0,
        PriorityMask = 1,
        AliasRegionAddress = 2,
        AliasRegionSize = 3,
        HeapRegionAddress = 4,
        HeapRegionSize = 5,
        TotalMemorySize = 6,
        UsedMemorySize = 7,
        DebuggerAttached = 8,
        ResourceLimit = 9,
        IdleTickCount = 10,
        RandomEntropy = 11,
        AslrRegionAddress = 12,
        AslrRegionSize = 13,
        StackRegionAddress = 14,
        StackRegionSize = 15,
        SystemResourceSizeTotal = 16,
        SystemResourceSizeUsed = 17,
        ProgramId = 18,
        InitialProcessIdRange = 19,
        UserExceptionContextAddress = 20,
        TotalNonSystemMemorySize = 21,
        UsedNonSystemMemorySize = 22,
        IsApplication = 23,
        ThreadTickCount = 0xf0000002
    }
}
