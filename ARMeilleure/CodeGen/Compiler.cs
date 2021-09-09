using ARMeilleure.CodeGen.X86;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;

namespace ARMeilleure.CodeGen
{
    static class Compiler
    {
        public static CompiledFunction Compile(
            ControlFlowGraph cfg,
            OperandType[] argTypes,
            OperandType retType,
            CompilerOptions options)
        {
            return Compile(new CompilerContext(cfg, argTypes, retType, options));
        }

        public static CompiledFunction Compile(CompilerContext context)
        {
            ControlFlowGraph cfg = context.Cfg;
            CompilerOptions options = context.Options;

            Logger.StartPass(PassName.Dominance);

            if ((options & CompilerOptions.SsaForm) != 0)
            {
                Dominance.FindDominators(cfg);
                Dominance.FindDominanceFrontiers(cfg);
            }

            Logger.EndPass(PassName.Dominance);

            Logger.StartPass(PassName.SsaConstruction);

            if ((options & CompilerOptions.SsaForm) != 0)
            {
                Ssa.Construct(cfg);
            }
            else
            {
                RegisterToLocal.Rename(cfg);
            }

            Logger.EndPass(PassName.SsaConstruction, cfg);

            return CodeGenerator.Generate(context);
        }
    }
}