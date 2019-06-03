namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct RegisterMasks
    {
        public int IntAvailableRegisters   { get; }
        public int IntCallerSavedRegisters { get; }
        public int IntCalleeSavedRegisters { get; }

        public RegisterMasks(
            int intAvailableRegisters,
            int intCallerSavedRegisters,
            int intCalleeSavedRegisters)
        {
            IntAvailableRegisters   = intAvailableRegisters;
            IntCallerSavedRegisters = intCallerSavedRegisters;
            IntCalleeSavedRegisters = intCalleeSavedRegisters;
        }
    }
}