using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using Silk.NET.OpenGL.Legacy;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.OpenGL
{
    class PersistentBuffers : IDisposable
    {
        private readonly GL _api;
        private readonly PersistentBuffer _main;
        private readonly PersistentBuffer _background;

        private readonly Dictionary<BufferHandle, IntPtr> _maps = new();

        public PersistentBuffer Default => BackgroundContextWorker.InBackground ? _background : _main;

        public PersistentBuffers(GL api)
        {
            _api = api;
            _main = new(_api);
            _background = new(_api);
        }

        public void Dispose()
        {
            _main?.Dispose();
            _background?.Dispose();
        }

        public unsafe void Map(BufferHandle handle, int size)
        {
            _api.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle.ToUInt32());
            void* ptr = _api.MapBufferRange(BufferTargetARB.CopyWriteBuffer, IntPtr.Zero, (uint)size, MapBufferAccessMask.ReadBit | MapBufferAccessMask.PersistentBit);

            _maps[handle] = (IntPtr)ptr;
        }

        public void Unmap(BufferHandle handle)
        {
            if (_maps.ContainsKey(handle))
            {
                _api.BindBuffer(BufferTargetARB.CopyWriteBuffer, handle.ToUInt32());
                _api.UnmapBuffer(BufferTargetARB.CopyWriteBuffer);

                _maps.Remove(handle);
            }
        }

        public bool TryGet(BufferHandle handle, out IntPtr ptr)
        {
            return _maps.TryGetValue(handle, out ptr);
        }
    }

    class PersistentBuffer : IDisposable
    {
        private readonly GL _api;
        private IntPtr _bufferMap;
        private uint _copyBufferHandle;
        private int _copyBufferSize;

        private byte[] _data;
        private IntPtr _dataMap;

        public PersistentBuffer(GL api)
        {
            _api = api;
        }

        private unsafe void EnsureBuffer(int requiredSize)
        {
            if (_copyBufferSize < requiredSize && _copyBufferHandle != 0)
            {
                _api.DeleteBuffer(_copyBufferHandle);

                _copyBufferHandle = 0;
            }

            if (_copyBufferHandle == 0)
            {
                _copyBufferHandle = _api.GenBuffer();
                _copyBufferSize = requiredSize;

                _api.BindBuffer(BufferTargetARB.CopyWriteBuffer, _copyBufferHandle);
                _api.BufferStorage(BufferStorageTarget.CopyWriteBuffer, (uint)requiredSize, in IntPtr.Zero, BufferStorageMask.MapReadBit | BufferStorageMask.MapPersistentBit);

                _bufferMap = (IntPtr)_api.MapBufferRange(BufferTargetARB.CopyWriteBuffer, IntPtr.Zero, (uint)requiredSize, MapBufferAccessMask.ReadBit | MapBufferAccessMask.PersistentBit);
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
            _api.MemoryBarrier(MemoryBarrierMask.ClientMappedBufferBarrierBit);

            IntPtr sync = _api.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None);
            GLEnum syncResult = _api.ClientWaitSync(sync, SyncObjectMask.Bit, 1000000000);

            if (syncResult == GLEnum.TimeoutExpired)
            {
                Logger.Error?.PrintMsg(LogClass.Gpu, $"Failed to sync persistent buffer state within 1000ms. Continuing...");
            }

            _api.DeleteSync(sync);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size)
        {
            EnsureBuffer(size);

            _api.BindBuffer(BufferTargetARB.PixelPackBuffer, _copyBufferHandle);

            view.WriteToPbo(0, false);

            _api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public unsafe ReadOnlySpan<byte> GetTextureData(TextureView view, int size, int layer, int level)
        {
            EnsureBuffer(size);

            _api.BindBuffer(BufferTargetARB.PixelPackBuffer, _copyBufferHandle);

            int offset = view.WriteToPbo2D(0, layer, level);

            _api.BindBuffer(BufferTargetARB.PixelPackBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size)[offset..];
        }

        public unsafe ReadOnlySpan<byte> GetBufferData(BufferHandle buffer, int offset, int size)
        {
            EnsureBuffer(size);

            _api.BindBuffer(BufferTargetARB.CopyReadBuffer, buffer.ToUInt32());
            _api.BindBuffer(BufferTargetARB.CopyWriteBuffer, _copyBufferHandle);

            _api.CopyBufferSubData(CopyBufferSubDataTarget.CopyReadBuffer, CopyBufferSubDataTarget.CopyWriteBuffer, offset, IntPtr.Zero, (uint)size);

            _api.BindBuffer(BufferTargetARB.CopyWriteBuffer, 0);

            Sync();

            return new ReadOnlySpan<byte>(_bufferMap.ToPointer(), size);
        }

        public void Dispose()
        {
            if (_copyBufferHandle != 0)
            {
                _api.DeleteBuffer(_copyBufferHandle);
            }
        }
    }
}
