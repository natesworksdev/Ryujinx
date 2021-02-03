using Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations;

namespace Ryujinx.HLE.HOS.Tamper.Atmosphere
{
    internal class Pointer : IOperand
    {
        private IOperand _position;
        private ITamperedProcess _process;

        public Pointer(IOperand position, ITamperedProcess process)
        {
            _position = position;
            _process = process;
        }

        public T Get<T>() where T : unmanaged
        {
            return _process.ReadMemory<T>(_position.Get<ulong>());
        }

        public void Set<T>(T value) where T : unmanaged
        {
            _process.WriteMemory(_position.Get<ulong>(), value);
        }
    }
}
