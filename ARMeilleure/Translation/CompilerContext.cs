using ARMeilleure.IntermediateRepresentation;

namespace ARMeilleure.Translation
{
    struct CompilerContext
    {
        public ControlFlowGraph Cfg { get; }

        public OperandType FuncReturnType { get; }

        public CompilerContext(ControlFlowGraph cfg, OperandType funcReturnType)
        {
            Cfg            = cfg;
            FuncReturnType = funcReturnType;
        }
    }
}