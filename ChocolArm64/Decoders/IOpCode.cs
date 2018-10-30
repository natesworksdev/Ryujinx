using ChocolArm64.Instructions;
using ChocolArm64.State;

namespace ChocolArm64.Decoders
{
    interface IOpCode
    {
        long Position { get; }

        InstEmitter  Emitter      { get; }
        RegisterSize RegisterSize { get; }
    }
}