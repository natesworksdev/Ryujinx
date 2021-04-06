using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.Translation
{
    readonly struct CompilerContext
    {
        public readonly ControlFlowGraph Cfg;
        public readonly OperandType[] FuncArgTypes;
        public readonly OperandType FuncReturnType;

        public readonly CompilerOptions Options;

        public CompilerContext(
            ControlFlowGraph cfg,
            OperandType[]    funcArgTypes,
            OperandType      funcReturnType,
            CompilerOptions  options)
        {
            Cfg            = cfg;
            FuncArgTypes   = funcArgTypes;
            FuncReturnType = funcReturnType;
            Options        = options;
        }
    }
}