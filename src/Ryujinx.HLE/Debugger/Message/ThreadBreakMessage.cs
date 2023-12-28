using IExecutionContext = Ryujinx.Cpu.IExecutionContext;

namespace Ryujinx.HLE.Debugger
{
    public class ThreadBreakMessage : IMessage
    {
        public IExecutionContext Context { get; }
        public ulong Address { get; }
        public int Opcode { get; }

        public ThreadBreakMessage(IExecutionContext context, ulong address, int opcode)
        {
            Context = context;
            Address = address;
            Opcode = opcode;
        }
    }
}
