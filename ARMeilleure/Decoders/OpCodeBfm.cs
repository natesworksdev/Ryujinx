namespace ARMeilleure.Decoders
{
    class OpCodeBfm : OpCodeAlu
    {
        public long WMask { get; }
        public long TMask { get; }
        public int  Pos   { get; }
        public int  Shift { get; }

        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeBfm(inst, address, opCode, inITBlock);

        public OpCodeBfm(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            var bm = DecoderHelper.DecodeBitMask(opCode, false);

            if (bm.IsUndefined)
            {
                Instruction = InstDescriptor.Undefined;

                return;
            }

            WMask = bm.WMask;
            TMask = bm.TMask;
            Pos   = bm.Pos;
            Shift = bm.Shift;
        }
    }
}