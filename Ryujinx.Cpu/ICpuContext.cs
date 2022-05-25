namespace Ryujinx.Cpu
{
    public interface ICpuContext
    {
        IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks);
        void Execute(IExecutionContext context, ulong address);
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}
