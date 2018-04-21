using ChocolArm64;

namespace Ryujinx.Core.OsHle.Handles
{
    class KThread : KSynchronizationObject
    {
        public AThread Thread { get; private set; }

        public KThread NextMutexThread   { get; set; }
        public KThread NextCondVarThread { get; set; }

        public long MutexAddress   { get; set; }
        public long CondVarAddress { get; set; }

        public int ProcessorId  { get; private set; }

        public int Priority { get; private set; }

        private int DesiredPriority;

        public int WaitHandle { get; set; }

        public int ThreadId => Thread.ThreadId;

        public KThread(AThread Thread, int ProcessorId, int Priority)
        {
            this.Thread      = Thread;
            this.ProcessorId = ProcessorId;

            SetPriority(Priority);
        }

        public void SetPriority(int Priority)
        {
            this.Priority = DesiredPriority = Priority;

            UpdatePriority();
        }

        public void ResetPriority()
        {
            Priority = DesiredPriority;
        }

        public void UpdatePriority()
        {
            if (Priority > (NextMutexThread?.Priority ?? Priority))
            {
                Priority = NextMutexThread.Priority;
            }
        }
    }
}