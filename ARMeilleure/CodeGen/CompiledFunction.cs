using ARMeilleure.CodeGen.Unwinding;

namespace ARMeilleure.CodeGen
{
    readonly struct CompiledFunction
    {
        public readonly byte[] Code { get; }

        public readonly UnwindInfo UnwindInfo { get; }

        public CompiledFunction(byte[] code, UnwindInfo unwindInfo)
        {
            Code       = code;
            UnwindInfo = unwindInfo;
        }
    }
}