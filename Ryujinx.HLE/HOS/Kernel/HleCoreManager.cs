using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    class HleCoreManager
    {
        private ConcurrentDictionary<Thread, ManualResetEvent> _threads;

        public HleCoreManager()
        {
            _threads = new ConcurrentDictionary<Thread, ManualResetEvent>();
        }

        public ManualResetEvent GetThread(Thread thread)
        {
            return _threads.GetOrAdd(thread, (key) => new ManualResetEvent(false));
        }

        public void RemoveThread(Thread thread)
        {
            if (_threads.TryRemove(thread, out ManualResetEvent Event))
            {
                Event.Set();
                Event.Dispose();
            }
        }
    }
}