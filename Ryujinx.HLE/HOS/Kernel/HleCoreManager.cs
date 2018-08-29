using System.Collections.Concurrent;
using System.Threading;

namespace Ryujinx.HLE.HOS.Kernel
{
    class HleCoreManager
    {
        public class PausableThread
        {
            private ManualResetEvent WaitEvent;

            public PausableThread()
            {
                WaitEvent = new ManualResetEvent(false);
            }

            public void Pause()
            {
                WaitEvent.Reset();
            }

            public void Unpause()
            {
                WaitEvent.Set();
            }

            public void Wait()
            {
                WaitEvent.WaitOne();
            }
        }

        private ConcurrentDictionary<Thread, PausableThread> Threads;

        public HleCoreManager()
        {
            Threads = new ConcurrentDictionary<Thread, PausableThread>();
        }

        public PausableThread GetThread(Thread Thread)
        {
            return Threads.GetOrAdd(Thread, (Key) => new PausableThread());
        }

        public void RemoveThread(Thread Thread)
        {
            if (Threads.TryRemove(Thread, out PausableThread Value))
            {
                Value.Unpause();

                //TODO: Dispose
            }
        }
    }
}