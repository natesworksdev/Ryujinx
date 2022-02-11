using ARMeilleure.Instructions;
using System;
using System.Numerics;

namespace ARMeilleure.Decoders
{
    class OpCodeT16MemStack : OpCodeT16, IOpCode32MemMult
    {
        public int Rn => 13;
        public int RegisterMask { get; }
        public int PostOffset { get; }
        public bool IsLoad { get; }
        public int Offset { get; }
        
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode, bool inITBlock) => new OpCodeT16MemStack(inst, address, opCode, inITBlock);

        public OpCodeT16MemStack(InstDescriptor inst, ulong address, int opCode, bool inITBlock) : base(inst, address, opCode, inITBlock)
        {
            int extra = (opCode >> 8) & 1;
            int regCount = BitOperations.PopCount((uint)opCode & 0x1ff);
            
            switch (inst.Name)
            {
                case InstName.Push:
                    RegisterMask = (opCode & 0xff) | (extra << 14);
                    IsLoad = false;
                    Offset = -4 * regCount;
                    PostOffset = -4 * regCount;
                    break;
                case InstName.Pop:
                    RegisterMask = (opCode & 0xff) | (extra << 15);
                    IsLoad = true;
                    Offset = 0;
                    PostOffset = 4 * regCount;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}