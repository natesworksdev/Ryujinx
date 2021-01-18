using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class Pointer : IOperand
    {
        private IOperand _position;
        private Parameter<IVirtualMemoryManager> _memory;

        public Pointer(IOperand position, Parameter<IVirtualMemoryManager> memory)
        {
            _position = position;
            _memory = memory;
        }

        public T Get<T>() where T : unmanaged
        {
            return _memory.Value.Read<T>(_position.Get<ulong>());
        }

        public void Set<T>(T value) where T : unmanaged
        {
            _memory.Value.Write(_position.Get<ulong>(), value);
        }
    }
}
