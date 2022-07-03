using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using static Ryujinx.Common.Memory.PartialUnmaps.PartialUnmapHelpers;

namespace Ryujinx.Common.Memory.PartialUnmaps
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PartialUnmapState
    {
        public NativeReaderWriterLock PartialUnmapLock;
        public int PartialUnmapsCount;
        public ThreadLocalMap<int> LocalCounts;
        public int ExceptionHandlerCount;
        public int ExceptionDoneCount;

        public readonly static int PartialUnmapLockOffset;
        public readonly static int PartialUnmapsCountOffset;
        public readonly static int LocalCountsOffset;
        public readonly static int ExceptionHandlerCountOffset;
        public readonly static int ExceptionDoneCountOffset;

        public readonly static IntPtr GlobalState;

        [DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenThread(int dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetExitCodeThread(IntPtr hThread, out uint lpExitCode);

        static unsafe PartialUnmapState()
        {
            PartialUnmapState instance = new PartialUnmapState();

            PartialUnmapLockOffset = OffsetOf(ref instance, ref instance.PartialUnmapLock);
            PartialUnmapsCountOffset = OffsetOf(ref instance, ref instance.PartialUnmapsCount);
            LocalCountsOffset = OffsetOf(ref instance, ref instance.LocalCounts);
            ExceptionHandlerCountOffset = OffsetOf(ref instance, ref instance.ExceptionHandlerCount);
            ExceptionDoneCountOffset = OffsetOf(ref instance, ref instance.ExceptionDoneCount);

            int size = Unsafe.SizeOf<PartialUnmapState>();
            GlobalState = Marshal.AllocHGlobal(size);
            Unsafe.InitBlockUnaligned((void*)GlobalState, 0, (uint)size);
        }

        public static unsafe void Reset()
        {
            int size = Unsafe.SizeOf<PartialUnmapState>();
            Unsafe.InitBlockUnaligned((void*)GlobalState, 0, (uint)size);
        }

        public static unsafe ref PartialUnmapState GetRef()
        {
            return ref Unsafe.AsRef<PartialUnmapState>((void*)GlobalState);
        }

        public bool RetryFromAccessViolation()
        {
            PartialUnmapLock.AcquireReaderLock();

            int threadID = GetCurrentThreadId();
            int threadIndex = LocalCounts.GetOrReserve(threadID, 0);

            if (threadIndex == -1)
            {
                // Out of thread local space... try again later.

                PartialUnmapLock.ReleaseReaderLock();

                return true;
            }

            ref int threadLocalPartialUnmapsCount = ref LocalCounts.GetValue(threadIndex);

            bool retry = threadLocalPartialUnmapsCount != PartialUnmapsCount;
            if (retry)
            {
                threadLocalPartialUnmapsCount = PartialUnmapsCount;
            }
            else
            {
                //LocalCounts.Release(threadID);
            }

            PartialUnmapLock.ReleaseReaderLock();

            return retry;
        }

        public void TrimThreads()
        {
            Span<int> ids = LocalCounts.ThreadIds.ToSpan();

            for (int i = 0; i < ids.Length; i++)
            {
                int id = ids[i];
                
                if (id != 0)
                {
                    IntPtr handle = OpenThread(0x40, false, (uint)id);

                    if (handle == IntPtr.Zero)
                    {
                        Interlocked.CompareExchange(ref ids[i], 0, id);
                    }
                    else
                    {
                        uint ExitCodeStillActive = 259;
                        GetExitCodeThread(handle, out uint exitCode);

                        if (exitCode != ExitCodeStillActive)
                        {
                            Interlocked.CompareExchange(ref ids[i], 0, id);
                        }

                        CloseHandle(handle);
                    }
                }
            }
        }
    }
}
