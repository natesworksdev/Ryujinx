using ChocolArm64.Instruction;
using ChocolArm64.State;
using System;

namespace ChocolArm64.Decoder
{
    internal class AOpCode : IAOpCode
    {
        public long Position  { get; private set; }
        public int  RawOpCode { get; private set; }

        public AInstEmitter     Emitter      { get; protected set; }
        public AInstInterpreter Interpreter  { get; protected set; }
        public ARegisterSize    RegisterSize { get; protected set; }

        public AOpCode(AInst inst, long position, int opCode)
        {
            this.Position  = position;
            this.RawOpCode = opCode;

            RegisterSize = ARegisterSize.Int64;

            Emitter     = inst.Emitter;
            Interpreter = inst.Interpreter;
        }

        public int GetBitsCount()
        {
            switch (RegisterSize)
            {
                case ARegisterSize.Int32:   return 32;
                case ARegisterSize.Int64:   return 64;
                case ARegisterSize.Simd64:  return 64;
                case ARegisterSize.Simd128: return 128;
            }

            throw new InvalidOperationException();
        }
    }
}