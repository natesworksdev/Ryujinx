namespace ARMeilleure.IntermediateRepresentation
{
    class PhiNode : Node
    {
        private BasicBlock[] _blocks;

        public PhiNode(Operand dest, int predecessorsCount) : base(predecessorsCount)
        {
            Sources = new Operand[predecessorsCount];

            _blocks = new BasicBlock[predecessorsCount];

            Dest = dest;
        }

        public BasicBlock GetBlock(int index)
        {
            return _blocks[index];
        }

        public void SetBlock(int index, BasicBlock block)
        {
            _blocks[index] = block;
        }
    }
}