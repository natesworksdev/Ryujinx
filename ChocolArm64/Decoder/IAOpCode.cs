using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    interface IaOpCode
    {
        long Position { get; }

        AInstEmitter  Emitter      { get; }
        ARegisterSize RegisterSize { get; }
    }
}