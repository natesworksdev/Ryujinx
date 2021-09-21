using OpenTK.Graphics.OpenGL;
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

        public int Query { get; }

        private int _buffer;
        private IntPtr _bufferMap;
        private QueryTarget _type;

        private unsafe int Create(QueryTarget type)
        {
            int handle;

            GL.CreateQueries(type, 1, &handle);

            return handle;
        }

        public BufferedQuery(QueryTarget type)
        {
            _buffer = Buffer.Create().ToInt32();
            Query = Create(type);
            _type = type;

            unsafe
            {
                long defaultValue = DefaultValue;
                GL.NamedBufferStorage(_buffer, sizeof(long), (IntPtr)(&defaultValue), BufferStorageFlags.MapReadBit | BufferStorageFlags.MapWriteBit | BufferStorageFlags.MapPersistentBit);
            }
            _bufferMap = GL.MapNamedBufferRange(_buffer, IntPtr.Zero, sizeof(long), BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit);
        }

        public void Reset()
        {
            GL.EndQuery(_type);
            GL.BeginQuery(_type, Query);
        }

        public void Begin()
        {
            GL.BeginQuery(_type, Query);
        }

        public unsafe void End(bool withResult)
        {
            GL.EndQuery(_type);

            if (withResult)
            {
                Marshal.WriteInt64(_bufferMap, -1L);
                GL.GetQueryBufferObject(Query, _buffer, QueryObjectParameterName.QueryResult, IntPtr.Zero);
                GL.MemoryBarrier(MemoryBarrierFlags.QueryBufferBarrierBit | MemoryBarrierFlags.ClientMappedBufferBarrierBit);
            }
        }

        public bool TryGetResult(out long result)
        {
            result = Marshal.ReadInt64(_bufferMap);

            return result != DefaultValue;
        }

        public long AwaitResult(AutoResetEvent wakeSignal = null)
        {
            long data = DefaultValue;

            if (wakeSignal == null)
            {
                while (data == DefaultValue)
                {
                    data = Marshal.ReadInt64(_bufferMap);
                }
            }
            else
            {
                int iterations = 0;
                while (data == DefaultValue && iterations++ < MaxQueryRetries)
                {
                    data = Marshal.ReadInt64(_bufferMap);
                    if (data == DefaultValue)
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
            GL.UnmapNamedBuffer(_buffer);
            GL.DeleteBuffer(_buffer);
            GL.DeleteQuery(Query);
        }
    }
}
