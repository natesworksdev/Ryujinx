namespace Ryujinx.Cpu
{
    public interface ICpuContext
    {
        IExecutionContext CreateExecutionContext();
        void Execute(IExecutionContext context, ulong address);
        void InvalidateCacheRegion(ulong address, ulong size);
    }
}
