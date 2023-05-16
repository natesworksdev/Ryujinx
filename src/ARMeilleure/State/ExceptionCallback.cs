using System.Threading.Tasks;

namespace ARMeilleure.State
{
    public delegate void ExceptionCallbackNoArgs(ExecutionContext context);
    public delegate void ExceptionCallback(ExecutionContext context, ulong address, int id);
    public delegate Task ExceptionCallbackAsync(ExecutionContext context, ulong address, int id);
}