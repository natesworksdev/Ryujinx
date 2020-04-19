namespace ARMeilleure.CodeGen.X86
{
    readonly struct IntrinsicInfo
    {
        public readonly X86Instruction Inst { get; }
        public readonly IntrinsicType Type { get; }

        public IntrinsicInfo(X86Instruction inst, IntrinsicType type)
        {
            Inst = inst;
            Type = type;
        }
    }
}