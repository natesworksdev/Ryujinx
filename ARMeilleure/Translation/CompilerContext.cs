using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.Translation
{
    readonly struct CompilerContext
    {
        public readonly ControlFlowGraph Cfg { get; }

        public readonly OperandType[] FuncArgTypes   { get; }
        public readonly OperandType FuncReturnType { get; }

        public readonly CompilerOptions Options { get; }

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