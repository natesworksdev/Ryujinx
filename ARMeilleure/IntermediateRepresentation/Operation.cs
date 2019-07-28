namespace ARMeilleure.IntermediateRepresentation
{
    class Operation : Node
    {
        public Instruction Inst { get; private set; }

        public Operation(
            Instruction inst,
            Operand destination,
            params Operand[] sources) : base(destination, sources.Length)
        {
            Inst = inst;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
        }

        public Operation(
            Instruction inst,
            Operand[] destinations,
            Operand[] sources) : base(destinations, sources.Length)
        {
            Inst = inst;

            for (int index = 0; index < sources.Length; index++)
            {
                SetSource(index, sources[index]);
            }
        }

        public void TurnIntoCopy(Operand source)
        {
            Inst = Instruction.Copy;

            SetSources(new Operand[] { source });
        }
    }
}