using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.Translation
{
    struct CompilerContext
    {
        public ControlFlowGraph Cfg { get; }

        public OperandType[] FuncArgTypes   { get; }
        public OperandType   FuncReturnType { get; }

        public CompilerContext(
            ControlFlowGraph cfg,
            OperandType[]    funcArgTypes,
            OperandType      funcReturnType)
        {
            Cfg            = cfg;
            FuncArgTypes   = funcArgTypes;
            FuncReturnType = funcReturnType;
        }
    }
}