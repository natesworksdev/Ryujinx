using ChocolArm64.Instruction;
using ChocolArm64.State;

namespace ChocolArm64.Decoder
{
    interface IOpCode
    {
        long Position { get; }

        InstEmitter  Emitter      { get; }
        RegisterSize RegisterSize { get; }
    }
}