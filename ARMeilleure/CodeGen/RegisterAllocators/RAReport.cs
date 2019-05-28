namespace ARMeilleure.CodeGen.RegisterAllocators
{
    struct RAReport
    {
        public int UsedRegisters;

        public RAReport(int usedRegisters)
        {
            UsedRegisters = usedRegisters;
        }
    }
}