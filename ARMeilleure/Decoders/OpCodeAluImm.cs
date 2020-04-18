using System;

namespace ARMeilleure.Decoders
{
    class OpCodeAluImm : OpCodeAlu, IOpCodeAluImm
    {
        public long Immediate { get; private set; }

        public OpCodeAluImm(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            switch (DataOp)
            {
                case DataOp.Arithmetic:
                    int shift = ((opCode >> 22) & 3) * 12;
                    Immediate = ((opCode >> 10) & 0xfff) << shift;
                    break;

                case DataOp.Logical:
                    var bm = DecoderHelper.DecodeBitMask(opCode, true);

                    if (bm.IsUndefined)
                    {
                        Instruction = InstDescriptor.Undefined;
                        return;
                    }

                    Immediate = bm.WMask;
                    break;

                default:
                    throw new ArgumentException(nameof(opCode));
            }
        }
    }
}