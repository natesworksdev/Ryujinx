using ChocolArm64.Decoder;
using ChocolArm64.Memory;
using ChocolArm64.State;

namespace ChocolArm64.Instruction
{
    delegate void InstInterpreter(AThreadState state, AMemory memory, AOpCode opCode);
}