using ARMeilleure.CodeGen;
using ARMeilleure.CodeGen.X86;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using System;
using System.Runtime.InteropServices;

namespace ARMeilleure.Translation
{
    static class Compiler
    {
        public static T Compile<T>(ControlFlowGraph cfg, OperandType funcReturnType)
        {
            IntPtr codePtr = JitCache.Map(Compile(cfg, funcReturnType));

            return Marshal.GetDelegateForFunctionPointer<T>(codePtr);
        }

        public static CompiledFunction Compile(ControlFlowGraph cfg, OperandType funcReturnType)
        {
            Logger.StartPass(PassName.Dominance);

            Dominance.FindDominators(cfg);
            Dominance.FindDominanceFrontiers(cfg);

            Logger.EndPass(PassName.Dominance);

            Logger.StartPass(PassName.SsaConstruction);

            Ssa.Rename(cfg);

            Logger.EndPass(PassName.SsaConstruction, cfg);

            CompilerContext cctx = new CompilerContext(cfg, funcReturnType);

            return CodeGenerator.Generate(cctx);
        }
    }
}