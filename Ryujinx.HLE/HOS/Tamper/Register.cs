using Ryujinx.HLE.HOS.Tamper.Operations;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class Register : IOperand
    {
        private ulong _register = 0;

        public T Get<T>() where T : unmanaged
        {
            return (T)(dynamic)_register;
        }

        public void Set<T>(T value) where T : unmanaged
        {
            _register = (ulong)(dynamic)value;
        }
    }
}
