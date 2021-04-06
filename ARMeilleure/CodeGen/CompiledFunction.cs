using ARMeilleure.CodeGen.Unwinding;

namespace ARMeilleure.CodeGen
{
    readonly struct CompiledFunction
    {
        public readonly byte[] Code;

        public readonly UnwindInfo UnwindInfo;

        public CompiledFunction(byte[] code, in UnwindInfo unwindInfo)
        {
            Code       = code;
            UnwindInfo = unwindInfo;
        }
    }
}