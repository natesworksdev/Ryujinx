using Silk.NET.OpenGL.Legacy;
using Ryujinx.Common.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Graphics.OpenGL.Queries
{
    class BufferedQuery : IDisposable
    {
        private const int MaxQueryRetries = 5000;
        private const long DefaultValue = -1;
        private const ulong HighMask = 0xFFFFFFFF00000000;

        public uint Query { get; }

        private readonly uint _buffer;
        private readonly IntPtr _bufferMap;
        private readonly QueryTarget _type;
        private readonly GL _api;

        public BufferedQuery(GL api, QueryTarget type)
        {
            _api = api;
            _buffer = _api.GenBuffer();
            Query = _api.GenQuery();
            _type = type;

            _api.BindBuffer(BufferTargetARB.QueryBuffer, _buffer);

            unsafe
            {
                long defaultValue = DefaultValue;
                _api.BufferStorage(BufferStorageTarget.QueryBuffer, sizeof(long), (IntPtr)(&defaultValue), BufferStorageMask.MapReadBit | BufferStorageMask.MapWriteBit | BufferStorageMask.MapPersistentBit);
            }

            unsafe
            {
                _bufferMap = new IntPtr(_api.MapBufferRange(BufferTargetARB.QueryBuffer, IntPtr.Zero, sizeof(long), MapBufferAccessMask.ReadBit | MapBufferAccessMask.WriteBit | MapBufferAccessMask.PersistentBit));
            }
        }

        public void Reset()
        {
            _api.EndQuery(_type);
            _api.BeginQuery(_type, Query);
        }

        public void Begin()
        {
            _api.BeginQuery(_type, Query);
        }

        public unsafe void End(bool withResult)
        {
            _api.EndQuery(_type);

            if (withResult)
            {
                _api.BindBuffer(BufferTargetARB.QueryBuffer, _buffer);

                Marshal.WriteInt64(_bufferMap, -1L);
                _api.GetQueryObject(Query,QueryObjectParameterName.Result, (long*)0);
                _api.MemoryBarrier(MemoryBarrierMask.QueryBufferBarrierBit | MemoryBarrierMask.ClientMappedBufferBarrierBit);
            }
            else
            {
                // Dummy result, just return 0.
                Marshal.WriteInt64(_bufferMap, 0L);
            }
        }

        private static bool WaitingForValue(long data)
        {
            return data == DefaultValue ||
                ((ulong)data & HighMask) == (unchecked((ulong)DefaultValue) & HighMask);
        }

        public bool TryGetResult(out long result)
        {
            result = Marshal.ReadInt64(_bufferMap);

            return !WaitingForValue(result);
        }

        public long AwaitResult(AutoResetEvent wakeSignal = null)
        {
            long data = DefaultValue;

            if (wakeSignal == null)
            {
                while (WaitingForValue(data))
                {
                    data = Marshal.ReadInt64(_bufferMap);
                }
            }
            else
            {
                int iterations = 0;
                while (WaitingForValue(data) && iterations++ < MaxQueryRetries)
                {
                    data = Marshal.ReadInt64(_bufferMap);
                    if (WaitingForValue(data))
                    {
                        wakeSignal.WaitOne(1);
                    }
                }

                if (iterations >= MaxQueryRetries)
                {
                    Logger.Error?.Print(LogClass.Gpu, $"Error: Query result timed out. Took more than {MaxQueryRetries} tries.");
                }
            }

            return data;
        }

        public void Dispose()
        {
            _api.BindBuffer(BufferTargetARB.QueryBuffer, _buffer);
            _api.UnmapBuffer(BufferTargetARB.QueryBuffer);
            _api.DeleteBuffer(_buffer);
            _api.DeleteQuery(Query);
        }
    }
}
