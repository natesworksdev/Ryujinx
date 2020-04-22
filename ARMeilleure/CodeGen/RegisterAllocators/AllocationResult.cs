namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct AllocationResult
    {
        public readonly int IntUsedRegisters { get; }
        public readonly int VecUsedRegisters { get; }
        public readonly int SpillRegionSize  { get; }

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