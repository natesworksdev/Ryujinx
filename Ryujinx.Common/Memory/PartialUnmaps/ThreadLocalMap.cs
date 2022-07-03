using System.Runtime.InteropServices;
using System.Threading;

using static Ryujinx.Common.Memory.PartialUnmaps.PartialUnmapHelpers;

namespace Ryujinx.Common.Memory.PartialUnmaps
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ThreadLocalMap<T> where T : unmanaged
    {
        public const int MapSize = 20;

        public Array20<int> ThreadIds;
        public Array20<T> Structs;

        public static int ThreadIdsOffset;
        public static int StructsOffset;

        static ThreadLocalMap()
        {
            ThreadLocalMap<T> instance = new ThreadLocalMap<T>();

            ThreadIdsOffset = OffsetOf(ref instance, ref instance.ThreadIds);
            StructsOffset = OffsetOf(ref instance, ref instance.Structs);
        }

        public int GetOrReserve(int threadId, T initial)
        {
            // Try get a match first.

            for (int i = 0; i < MapSize; i++)
            {
                int compare = Interlocked.CompareExchange(ref ThreadIds[i], threadId, threadId);

                if (compare == threadId)
                {
                    return i;
                }
            }

            // Try get a free entry. Since the id is assumed to be unique to this thread, we know it doesn't exist yet.

            for (int i = 0; i < MapSize; i++)
            {
                int compare = Interlocked.CompareExchange(ref ThreadIds[i], threadId, 0);

                if (compare == 0)
                {
                    Structs[i] = initial;
                    return i;
                }
            }

            return -1;
        }

        public ref T GetValue(int index)
        {
            return ref Structs[index];
        }

        public void Release(int index)
        {
            Interlocked.Exchange(ref ThreadIds[index], 0);
        }
    }
}
