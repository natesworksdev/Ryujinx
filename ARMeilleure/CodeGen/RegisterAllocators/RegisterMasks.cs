namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct RegisterMasks
    {
        public int IntAvailableRegisters   { get; }
        public int IntCalleeSavedRegisters { get; }

        public RegisterMasks(int intAvailableRegisters, int intCalleeSavedRegisters)
        {
            IntAvailableRegisters   = intAvailableRegisters;
            IntCalleeSavedRegisters = intCalleeSavedRegisters;
        }
    }
}