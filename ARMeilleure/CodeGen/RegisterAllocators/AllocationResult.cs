namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct AllocationResult
    {
        public readonly int IntUsedRegisters;
        public readonly int VecUsedRegisters;
        public readonly int SpillRegionSize;

        public AllocationResult(
            int intUsedRegisters,
            int vecUsedRegisters,
            int spillRegionSize)
        {
            IntUsedRegisters = intUsedRegisters;
            VecUsedRegisters = vecUsedRegisters;
            SpillRegionSize  = spillRegionSize;
        }
    }
}