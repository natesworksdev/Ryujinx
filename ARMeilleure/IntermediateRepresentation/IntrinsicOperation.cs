namespace ARMeilleure.IntermediateRepresentation
{
    class IntrinsicOperation : Operation
    {
        public Intrinsic Intrinsic { get; }

        public IntrinsicOperation(Intrinsic intrin, Operand? destination, params Operand[] sources) : base(Instruction.Extended, destination, sources)
        {
            Intrinsic = intrin;
        }
    }
}