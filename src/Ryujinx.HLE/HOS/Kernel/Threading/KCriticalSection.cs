using System.Threading;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Kernel.Threading
{
    class KCriticalSection
    {
        private readonly KernelContext _context;
        private readonly object _lock;
        private int _recursionCount;

        public object Lock => _lock;

        public KCriticalSection(KernelContext context)
        {
            _context = context;
            _lock = new object();
        }

        public void Enter()
        {
            Monitor.Enter(_lock);

            _recursionCount++;
        }

        public void Leave()
        {
            if (_recursionCount == 0)
            {
                return;
            }
            else
            {
                _recursionCount--;
                Monitor.Exit(_lock);
            }
        }

    }
}
