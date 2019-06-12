namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct AllocationResult
    {
        public int IntUsedRegisters { get; }
        public int VecUsedRegisters { get; }
        public int SpillRegionSize  { get; }
        public int MaxCallArgs      { get; }

        public AllocationResult(
            int intUsedRegisters,
            int vecUsedRegisters,
            int spillRegionSize,
            int maxCallArgs)
        {
            IntUsedRegisters = intUsedRegisters;
            VecUsedRegisters = vecUsedRegisters;
            SpillRegionSize  = spillRegionSize;
            MaxCallArgs      = maxCallArgs;
        }
    }
}