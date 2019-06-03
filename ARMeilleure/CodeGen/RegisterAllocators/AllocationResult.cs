namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct AllocationResult
    {
        public int UsedRegisters   { get; }
        public int SpillRegionSize { get; }
        public int MaxCallArgs     { get; }

        public AllocationResult(int usedRegisters, int spillRegionSize, int maxCallArgs)
        {
            UsedRegisters   = usedRegisters;
            SpillRegionSize = spillRegionSize;
            MaxCallArgs     = maxCallArgs;
        }
    }
}