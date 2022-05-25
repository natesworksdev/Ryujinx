namespace Ryujinx.Cpu
{
    public delegate void ExceptionCallbackNoArgs(IExecutionContext context);
    public delegate void ExceptionCallback(IExecutionContext context, ulong address, int imm);

    public struct ExceptionCallbacks
    {
        public readonly ExceptionCallbackNoArgs InterruptCallback;
        public readonly ExceptionCallback BreakCallback;
        public readonly ExceptionCallback SupervisorCallback;
        public readonly ExceptionCallback UndefinedCallback;

        public ExceptionCallbacks(
            ExceptionCallbackNoArgs interruptCallback = null,
            ExceptionCallback breakCallback = null,
            ExceptionCallback supervisorCallback = null,
            ExceptionCallback undefinedCallback = null)
        {
            InterruptCallback = interruptCallback;
            BreakCallback = breakCallback;
            SupervisorCallback = supervisorCallback;
            UndefinedCallback = undefinedCallback;
        }
    }
}
