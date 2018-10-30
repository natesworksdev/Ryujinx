using ChocolArm64.Decoder;
using ChocolArm64.Memory;
using ChocolArm64.State;

namespace ChocolArm64.Instruction
{
    delegate void InstInterpreter(CpuThreadState state, MemoryManager memory, AOpCode opCode);
}