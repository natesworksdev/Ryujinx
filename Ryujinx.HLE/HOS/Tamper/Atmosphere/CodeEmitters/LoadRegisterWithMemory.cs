namespace Ryujinx.HLE.HOS.Tamper.Atmosphere.CodeEmitters
{
    /// <summary>
    /// Code type 5 allows loading a value from memory into a register, either using a fixed address or by
    /// dereferencing the destination register.
    /// </summary>
    class LoadRegisterWithMemory
    {
        private const int OperationWidthIndex = 1;
        private const int MemoryRegionIndex = 2;
        private const int DestinationRegisterIndex = 3;
        private const int OffsetImmediateIndex = 6;

        private const int OffsetImmediateSize = 10;

        public static void Emit(byte[] instruction, CompilationContext context)
        {
            // 5TMR00AA AAAAAAAA
            // T: Width of memory read (1, 2, 4, or 8 bytes).
            // M: Memory region to write to (0 = Main NSO, 1 = Heap).
            // R: Register to load value into.
            // A: Immediate offset to use from memory region base.

            byte operationWidth = instruction[OperationWidthIndex];
            MemoryRegion memoryRegion = (MemoryRegion)instruction[MemoryRegionIndex];
            Register destinationRegister = context.GetRegister(instruction[DestinationRegisterIndex]);
            ulong address = InstructionHelper.GetImmediate(instruction, OffsetImmediateIndex, OffsetImmediateSize);
            Pointer sourceMemory = MemoryHelper.EmitPointer(memoryRegion, address, context);

            InstructionHelper.EmitMov(operationWidth, context, destinationRegister, sourceMemory);
        }
    }
}
