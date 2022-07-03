using System.Runtime.InteropServices;
using System.Threading;

using static Ryujinx.Common.Memory.PartialUnmaps.PartialUnmapHelpers;

namespace Ryujinx.Common.Memory.PartialUnmaps
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeReaderWriterLock
    {
        public int WriteLock;
        public int ReaderCount;

        public static int WriteLockOffset;
        public static int ReaderCountOffset;

        static NativeReaderWriterLock()
        {
            NativeReaderWriterLock instance = new NativeReaderWriterLock();

            WriteLockOffset = OffsetOf(ref instance, ref instance.WriteLock);
            ReaderCountOffset = OffsetOf(ref instance, ref instance.ReaderCount);
        }

        public void AcquireReaderLock()
        {
            // Must take write lock for a very short time to become a reader.

            do
            {

            } while (Interlocked.CompareExchange(ref WriteLock, 1, 0) != 0);

            Interlocked.Increment(ref ReaderCount);

            Interlocked.Exchange(ref WriteLock, 0);
        }

        public void ReleaseReaderLock()
        {
            Interlocked.Decrement(ref ReaderCount);
        }

        public void UpgradeToWriterLock()
        {
            // Prevent any more threads from entering reader.
            // If the write lock is already taken, wait for it to not be taken.

            Interlocked.Decrement(ref ReaderCount);

            do
            {

            } while (Interlocked.CompareExchange(ref WriteLock, 1, 0) != 0);

            // Wait for reader count to drop to 0, then take the lock again as the only reader.

            do
            {

            } while (Interlocked.CompareExchange(ref ReaderCount, 1, 0) != 0);
        }

        public void DowngradeFromWriterLock()
        {
            // Release the WriteLock.

            Interlocked.Exchange(ref WriteLock, 0);
        }
    }
}
