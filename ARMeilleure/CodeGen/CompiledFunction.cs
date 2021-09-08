using ARMeilleure.CodeGen.Linking;
using ARMeilleure.CodeGen.Unwinding;
using ARMeilleure.Translation.Cache;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.CodeGen
{
    readonly struct CompiledFunction
    {
        public byte[] Code { get; }
        public UnwindInfo UnwindInfo { get; }
        public RelocInfo RelocInfo { get; }

        public CompiledFunction(byte[] code, UnwindInfo unwindInfo, RelocInfo relocInfo)
        {
            Code       = code;
            UnwindInfo = unwindInfo;
            RelocInfo  = relocInfo;
        }

        public T Map<T>()
        {
            IntPtr codePtr = JitCache.Map(this);

            return Marshal.GetDelegateForFunctionPointer<T>(codePtr);
        }
    }
}