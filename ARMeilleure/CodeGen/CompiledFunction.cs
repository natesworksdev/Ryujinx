using ARMeilleure.CodeGen.Unwinding;

namespace ARMeilleure.CodeGen
{
    readonly struct CompiledFunction
    {
        public byte[] Code { get; }

        public UnwindInfo UnwindInfo { get; }

        public CompiledFunction(byte[] code, in UnwindInfo unwindInfo)
        {
            Code       = code;
            UnwindInfo = unwindInfo;
        }
    }
}