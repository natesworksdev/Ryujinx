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
        public static T Compile<T>(
            ControlFlowGraph cfg,
            OperandType[]    funcArgTypes,
            OperandType      funcReturnType)
        {
            CompilerContext cctx = GetCompilerContext(cfg, funcArgTypes, funcReturnType);

            CompiledFunction func = CodeGenerator.Generate(cctx);

            IntPtr codePtr = JitCache.Map(func);

            return Marshal.GetDelegateForFunctionPointer<T>(codePtr);
        }

        private static CompilerContext GetCompilerContext(
            ControlFlowGraph cfg,
            OperandType[]    funcArgTypes,
            OperandType      funcReturnType)
        {
            Logger.StartPass(PassName.Dominance);

            Dominance.FindDominators(cfg);
            Dominance.FindDominanceFrontiers(cfg);

            Logger.EndPass(PassName.Dominance);

            Logger.StartPass(PassName.SsaConstruction);

            Ssa.Rename(cfg);

            Logger.EndPass(PassName.SsaConstruction, cfg);

            return new CompilerContext(cfg, funcArgTypes, funcReturnType);
        }
    }
}