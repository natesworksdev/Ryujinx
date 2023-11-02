using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Common.SystemInterop
{
    /// <summary>
    /// Similar to an AutoResetEvent, but with nanosecond timeout.
    /// Only one thread should call WaitOne.
    /// </summary>
    public partial class NanosleepEvent
    {
        public static bool IsSupported => OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid();

        private const string Libc = "libc";

        private const int SIGUSR2 = 12;
        private const int SIGCONT = 18;

        private const IntPtr SIG_IGN = (IntPtr)1;

        private const int EventStateIdle = 0;
        private const int EventStateSignalled = 1;
        private const int EventStateNanosleep = 2;
        private const int EventStateKilling = 3;
        private const int EventStateKilled = 4;

        private const long X64NanosleepBias = 50000;

        [StructLayout(LayoutKind.Sequential)]
        private struct Timespec
        {
            public long tv_sec;  // Seconds
            public long tv_nsec; // Nanoseconds
        }

        [LibraryImport(Libc, SetLastError = true)]
        private static partial int nanosleep(ref Timespec req, ref Timespec rem);

        [LibraryImport(Libc, SetLastError = true)]
        private static partial int kill(int pid, int sig);

        [LibraryImport(Libc, SetLastError = true)]
        private static partial int getpid();

        [LibraryImport(Libc, SetLastError = true)]
        private static unsafe partial int signal(int signum, delegate* unmanaged<void> callback);

        [LibraryImport("libc", SetLastError = true)]
        private static partial int sigaction(int signum, ref SigAction sigAction, out SigAction oldAction);

        [LibraryImport("libc", SetLastError = true)]
        private static partial int siginterrupt(int signum, int flag);

        [LibraryImport("libc", SetLastError = true)]
        private static partial IntPtr pthread_self();

        [LibraryImport("libc", SetLastError = true)]
        private static partial IntPtr pthread_kill(IntPtr thread, int sig);


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SigSet
        {
            fixed long sa_mask[16];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public unsafe struct SigAction
        {
            public delegate* unmanaged<void> sa_handler;
            public SigSet sa_mask;
            public int sa_flags;
            public IntPtr sa_restorer;
        }

        [LibraryImport("libc", SetLastError = true)]
        private static partial int sigemptyset(ref SigSet set);

        private const int SA_SIGINFO = 0x00000004;

        static unsafe NanosleepEvent()
        {
            // Technically _should_ use SIGUSR2, but we need to register a signal handler
            // and it isn't yet supported by all platforms.
            // Doing it manually breaks the runtime.
            /*PosixSignalRegistration.Create((PosixSignal)SIGUSR2, (context) => {
                Console.WriteLine($"shit?");
                context.Cancel = true;
            });*/
            /*
            SigAction sig = new()
            {
                sa_handler = &NanoSlippy,
                sa_flags = SA_SIGINFO | 0x10000000,
            };

            sigemptyset(ref sig.sa_mask);

            int result = sigaction(SIGUSR2, ref sig, out SigAction old);
            */
        }

        [UnmanagedCallersOnly]
        public static void NanoSlippy() {
            Console.WriteLine("Uh");
        }

        public NanosleepEvent(bool signalled)
        {
            _state = signalled ? EventStateSignalled : EventStateIdle;

            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                _bias = X64NanosleepBias;
            }
            else
            {
                _bias = 0;
            }
        }

        public static long MinTicks()
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                return Stopwatch.Frequency / 20000;
            }
            else
            {
                return Stopwatch.Frequency / 50000;
            }
        }

        private object _lock = new();
        private int _state = EventStateIdle;
        private IntPtr _waiterPid = 0;
        private long _bias;

        public void WaitOne(long nanoseconds)
        {
            //Console.WriteLine($"Waiting {nanoseconds}");
            if (_waiterPid == 0)
            {
                _waiterPid = pthread_self();
                //Interlocked.Exchange(ref _waiterPid, getpid());
            }

            int prevValue;
            do
            {
                // Might need to wait for a previous Signal() call to stop calling kill().
                prevValue = Interlocked.CompareExchange(ref _state, EventStateNanosleep, EventStateIdle);
            }
            while (prevValue == EventStateKilled);

            if (prevValue == EventStateIdle)
            {
                nanoseconds -= _bias;

                Timespec req = GetTimespecFromNanoseconds((ulong)nanoseconds);
                Timespec rem = new();

                //Console.WriteLine($"i waitito {getpid()}");
                int result = nanosleep(ref req, ref rem);
                //Console.WriteLine($"i finito?");

                prevValue = Interlocked.CompareExchange(ref _state, EventStateIdle, EventStateNanosleep);

                if (prevValue == EventStateKilling)
                {
                    // Make sure the signal loop has been told to stop.
                    Interlocked.CompareExchange(ref _state, EventStateKilled, EventStateKilling);
                }
            }
            //Console.WriteLine($"I got out...");

            Interlocked.Exchange(ref _state, EventStateIdle);
        }

        private static Timespec GetTimespecFromNanoseconds(ulong nanoseconds)
        {
            return new Timespec
            {
                tv_sec = (long)(nanoseconds / 1_000_000_000),
                tv_nsec = (long)(nanoseconds % 1_000_000_000)
            };
        }

        public void Signal()
        {
            lock (_lock)
            {
                int prevValue = Interlocked.CompareExchange(ref _state, EventStateSignalled, EventStateIdle);

                if (prevValue == EventStateNanosleep)
                {
                    // If the timer is nanosleeping, repeatedly signal until it wakes.
                    // This is to account for a small race condition where the state is set to Nanosleep,
                    // but the nanosleep has not started yet

                    prevValue = Interlocked.CompareExchange(ref _state, EventStateKilling, EventStateNanosleep);

                    if (prevValue == EventStateNanosleep) {
                        SpinWait wait = new();

                        do
                        {
                            pthread_kill(_waiterPid, SIGCONT);
                            wait.SpinOnce();
                        }
                        while (Volatile.Read(ref _state) == EventStateKilling);

                        Interlocked.CompareExchange(ref _state, EventStateIdle, EventStateKilled);
                    }
                }
            }
        }
    }
}