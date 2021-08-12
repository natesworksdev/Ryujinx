using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Graphics.Vulkan.Queries
{
    class BufferedQuery : IDisposable
    {
        private const int MaxQueryRetries = 5000;
        private const long DefaultValue = -1;

        private readonly Vk _api;
        private readonly Device _device;
        private readonly PipelineFull _pipeline;

        private QueryPool _queryPool;

        private readonly BufferHolder _buffer;
        private readonly IntPtr _bufferMap;
        private readonly CounterType _type;

        public unsafe BufferedQuery(VulkanGraphicsDevice gd, Device device, PipelineFull pipeline, CounterType type)
        {
            _api = gd.Api;
            _device = device;
            _pipeline = pipeline;

            var queryPoolCreateInfo = new QueryPoolCreateInfo()
            {
                SType = StructureType.QueryPoolCreateInfo,
                QueryCount = 1,
                QueryType = GetQueryType(type)
            };

            gd.Api.CreateQueryPool(device, queryPoolCreateInfo, null, out _queryPool).ThrowOnError();

            var buffer = gd.BufferManager.Create(gd, sizeof(long), forConditionalRendering: true);

            _bufferMap = buffer.Map(0, sizeof(long));
            Marshal.WriteInt64(_bufferMap, -1L);
            _buffer = buffer;
        }

        private static QueryType GetQueryType(CounterType type)
        {
            return type switch
            {
                CounterType.SamplesPassed => QueryType.Occlusion,
                CounterType.PrimitivesGenerated => QueryType.PipelineStatistics,
                CounterType.TransformFeedbackPrimitivesWritten => QueryType.TransformFeedbackStreamExt,
                _ => QueryType.Occlusion
            };
        }

        public Auto<DisposableBuffer> GetBuffer()
        {
            return _buffer.GetBuffer();
        }

        public void Reset()
        {
            End(false);
            Begin();
        }

        public void Begin()
        {
            _pipeline.BeginQuery(_queryPool);
        }

        public unsafe void End(bool withResult)
        {
            _pipeline.EndQuery(_queryPool);

            if (withResult)
            {
                Marshal.WriteInt64(_bufferMap, -1L);
                _pipeline.CopyQueryResults(_queryPool, _buffer);
                // _pipeline.FlushCommandsImpl();
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

        public unsafe void Dispose()
        {
            _buffer.Dispose();
            _api.DestroyQueryPool(_device, _queryPool, null);
            _queryPool = default;
        }
    }
}
