using ARMeilleure.State;

namespace Ryujinx.HLE.Debugger
{
    public class ThreadBreakMessage : IMessage
    {
        public InstExceptionEventArgs EventArgs { get; }
        public ulong ThreadUid { get; }

        public ThreadBreakMessage(InstExceptionEventArgs eventArgs, ulong threadUid)
        {
            EventArgs = eventArgs;
            ThreadUid = threadUid;
        }
    }
}