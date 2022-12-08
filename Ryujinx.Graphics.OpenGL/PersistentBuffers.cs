using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;

namespace Ryujinx.Graphics.OpenGL
{
    class PersistentBuffers : IDisposable
    {
        private PersistentBuffer _main = new PersistentBuffer();
        private PersistentBuffer _background = new PersistentBuffer();

        public PersistentBuffer Default => BackgroundContextWorker.InBackground ? _background : _main;
        public PersistentTextureBuffer Textures = new PersistentTextureBuffer();

        public void Dispose()
        {
            _main?.Dispose();
            _background?.Dispose();

            Textures?.Dispose();
        }
    }

    class PersistentTextureBuffer : IDisposable
    {
        public const int TextureMaxSize = 16 * 1024 * 1024;
        public const int TextureMaxNumber = 3;

        private const int CopyBufferSize = TextureMaxNumber * TextureMaxSize;

        private IntPtr _bufferMap;
        private IntPtr[] _syncs;
        private int _copyBufferHandle = 0;
        private int _currentIndex = 0;
        private int _currentOffset = 0;

        private void Init()
        {
            if (_copyBufferHandle == 0)
            {
                _copyBufferHandle = GL.GenBuffer();

                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _copyBufferHandle);
                GL.BufferStorage(BufferTarget.CopyWriteBuffer, CopyBufferSize, IntPtr.Zero, BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit | BufferStorageFlags.MapCoherentBit | BufferStorageFlags.ClientStorageBit);

                _bufferMap = GL.MapBufferRange(BufferTarget.CopyWriteBuffer, IntPtr.Zero, CopyBufferSize, BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit);
                _syncs = new IntPtr[TextureMaxNumber];

                for (int i = 0; i < TextureMaxNumber; i++)
                {
                    _syncs[i] = IntPtr.Zero;
                }
            }
        }

        public unsafe void SetTextureData(TextureView view, ReadOnlySpan<byte> data, int size, int layer = -1, int level = -1, int width = -1, int height = -1)
        {
            Init();

            if (_currentOffset + size > TextureMaxSize)
            {
                _syncs[_currentIndex] = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
                _currentIndex = (_currentIndex + 1) % TextureMaxNumber;
                _currentOffset = 0;
            }

            if (_syncs[_currentIndex] != IntPtr.Zero)
            {
                WaitSyncStatus syncResult = GL.ClientWaitSync(_syncs[_currentIndex], ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000000);

                if (syncResult == WaitSyncStatus.TimeoutExpired)
                {
                    Logger.Error?.PrintMsg(LogClass.Gpu, $"Failed to sync persistent buffer state within 1000ms. Continuing...");
                }

                GL.DeleteSync(_syncs[_currentIndex]);
                _syncs[_currentIndex] = IntPtr.Zero;
            }

            int offset = _currentIndex * TextureMaxSize + _currentOffset;

            Span<byte> buffer = new Span<byte>((_bufferMap + offset).ToPointer(), size);
            data.CopyTo(buffer);

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, _copyBufferHandle);

            if (layer == -1)
            {
                view.ReadFromPbo(offset, size);
            }
            else
            {
                view.ReadFromPbo2D(offset, layer, level, width, height);
            }

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            _currentOffset += size;
        }

        public void Dispose()
        {
            if (_copyBufferHandle != 0)
            {
                GL.DeleteBuffer(_copyBufferHandle);
            }

            for (int i = 0; i < TextureMaxNumber; i++)
            {
                if (_syncs[i] != IntPtr.Zero)
                {
                    GL.ClientWaitSync(_syncs[i], ClientWaitSyncFlags.SyncFlushCommandsBit, 0);
                    GL.DeleteSync(_syncs[i]);
                }
            }
        }
    }

    class PersistentBuffer : IDisposable
    {
        private IntPtr _bufferMap;
        private int _copyBufferHandle;
        private int _copyBufferSize;

        private byte[] _data;
        private IntPtr _dataMap;

        private void EnsureBuffer(int requiredSize)
        {
            if (_copyBufferSize < requiredSize && _copyBufferHandle != 0)
            {
                GL.DeleteBuffer(_copyBufferHandle);

                _copyBufferHandle = 0;
            }

            if (_copyBufferHandle == 0)
            {
                _copyBufferHandle = GL.GenBuffer();
                _copyBufferSize = requiredSize;

                GL.BindBuffer(BufferTarget.CopyWriteBuffer, _copyBufferHandle);
                GL.BufferStorage(BufferTarget.CopyWriteBuffer, requiredSize, IntPtr.Zero, BufferStorageFlags.MapReadBit | BufferStorageFlags.MapPersistentBit);

                _bufferMap = GL.MapBufferRange(BufferTarget.CopyWriteBuffer, IntPtr.Zero, requiredSize, BufferAccessMask.MapReadBit | BufferAccessMask.MapPersistentBit);
            }
        }

        public unsafe IntPtr GetHostArray(int requiredSize)
        {
            if (_data == null || _data.Length < requiredSize)
            {
                _data = GC.AllocateUninitializedArray<byte>(requiredSize, true);

                _dataMap = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_data));
            }

            return _dataMap;
        }

        private void Sync()
        {
            GL.MemoryBarrier(MemoryBarrierFlags.ClientMappedBufferBarrierBit);

            IntPtr sync = GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None);
            WaitSyncStatus syncResult = GL.ClientWaitSync(sync, ClientWaitSyncFlags.SyncFlushCommandsBit, 1000000000);

            if (syncResult == WaitSyncStatus.TimeoutExpired)
            {
                Logger.Error?.PrintMsg(LogClass.Gpu, $"Failed to sync persistent buffer state within 1000ms. Continuing...");
            }

            GL.DeleteSync(sync);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyBufferHandle);

            view.WriteToPbo(0, false);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size, int layer, int level)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, _copyBufferHandle);

            int offset = view.WriteToPbo2D(0, layer, level);

            GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size).Slice(offset);
        }

        public unsafe ReadOnlySpan<byte> GetBufferData(BufferHandle buffer, int offset, int size)
        {
            EnsureBuffer(size);

            GL.BindBuffer(BufferTarget.CopyReadBuffer, buffer.ToInt32());
            GL.BindBuffer(BufferTarget.CopyWriteBuffer, _copyBufferHandle);

            GL.CopyBufferSubData(BufferTarget.CopyReadBuffer, BufferTarget.CopyWriteBuffer, (IntPtr)offset, IntPtr.Zero, size);

            GL.BindBuffer(BufferTarget.CopyWriteBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public void Dispose()
        {
            if (_copyBufferHandle != 0)
            {
                GL.DeleteBuffer(_copyBufferHandle);
            }
        }
    }
}
