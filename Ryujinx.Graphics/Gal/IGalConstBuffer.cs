using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalConstBuffer
    {
        void LockCache();
        void UnlockCache();

        void Create(long key, IntPtr hostAddress, int size);
        void Create(long key, byte[] buffer);

        bool IsCached(long Key, int Size);
    }
}