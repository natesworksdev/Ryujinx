using System.Threading;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class NvMap
    {
        public int  Handle;
        public int  Id;
        public int  Size;
        public int  Align;
        public int  Kind;
        public long CpuAddress;
        public long GpuAddress;

        private long m_RefCount;

        public long RefCount => m_RefCount;

        public NvMap()
        {
            m_RefCount = 1;
        }

        public NvMap(int Size) : this()
        {
            this.Size = Size;
        }

        public long IncrementRefCount()
        {
            return Interlocked.Increment(ref m_RefCount);
        }

        public long DecrementRefCount()
        {
            return Interlocked.Decrement(ref m_RefCount);
        }
    }
}