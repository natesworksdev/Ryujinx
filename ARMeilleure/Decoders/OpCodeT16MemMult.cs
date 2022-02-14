using ARMeilleure.Instructions;
using System;
using System.Numerics;

namespace ARMeilleure.Decoders
{
    class OpCodeT16MemMult : OpCodeT16, IOpCode32MemMult
    {
        public int Rn { get; }
        public int RegisterMask { get; }
        public int PostOffset { get; }
        public bool IsLoad { get; }
        public int Offset { get; }
        
        public new static OpCode Create(InstDescriptor inst, ulong address, int opCode) => new OpCodeT16MemMult(inst, address, opCode);

        public OpCodeT16MemMult(InstDescriptor inst, ulong address, int opCode) : base(inst, address, opCode)
        {
            RegisterMask = opCode & 0xff;
            Rn = (opCode >> 8) & 7;
            
            int regCount = BitOperations.PopCount((uint)RegisterMask);
            
            switch (inst.Name)
            {
                case InstName.Stm:
                    IsLoad = false;
                    Offset = 0;
                    PostOffset = 4 * regCount;
                    break;
                case InstName.Ldm:
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
