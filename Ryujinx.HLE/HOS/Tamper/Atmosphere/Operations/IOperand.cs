namespace Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations
{
    internal interface IOperand
    {
        public T Get<T>() where T : unmanaged;
        public void Set<T>(T value) where T : unmanaged;
    }
}
