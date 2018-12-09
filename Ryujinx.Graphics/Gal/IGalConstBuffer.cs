using System;

namespace Ryujinx.Graphics.Gal
{
    public interface IGalConstBuffer
    {
        void LockCache();
        void UnlockCache();

        void Create(long key, IntPtr hostAddress, long size);
        void Create(long key, byte[] data);

        bool IsCached(long Key, long Size);
    }
}