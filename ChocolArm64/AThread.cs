using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Threading;

namespace ChocolArm64
{
    public class AThread
    {
        public AThreadState ThreadState { get; private set; }
        public AMemory      Memory      { get; private set; }

        private ATranslator _translator;

        public Thread Work;

        public event EventHandler WorkFinished;

        private int _isExecuting;

        public AThread(ATranslator translator, AMemory memory, long entryPoint)
        {
            _translator = translator;
            Memory     = memory;

            ThreadState = new AThreadState();

            ThreadState.ExecutionMode = AExecutionMode.AArch64;

            ThreadState.Running = true;

            Work = new Thread(delegate()
            {
                translator.ExecuteSubroutine(this, entryPoint);

                memory.RemoveMonitor(ThreadState.Core);

                WorkFinished?.Invoke(this, EventArgs.Empty);
            });
        }

        public bool Execute()
        {
            if (Interlocked.Exchange(ref _isExecuting, 1) == 1)
            {
                return false;
            }

            Work.Start();

            return true;
        }

        public void StopExecution()
        {
            ThreadState.Running = false;
        }

        public void RequestInterrupt()
        {
            ThreadState.RequestInterrupt();
        }

        public bool IsCurrentThread()
        {
            return Thread.CurrentThread == Work;
        }
    }
}