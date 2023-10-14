using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Sdk.Sm
{
    class LmApi : IDisposable
    {
        public class Logger : IDisposable
        {
            private readonly HeapAllocator _allocator;
            private int _sessionHandle;

            public Logger(HeapAllocator allocator, int sessionHandle)
            {
                _allocator = allocator;
                _sessionHandle = sessionHandle;
            }

            public Result Log(Span<byte> message)
            {
                ulong bufferSize = (ulong)message.Length;
                ulong bufferAddress = _allocator.Allocate(bufferSize);

                HorizonStatic.AddressSpace.Write(bufferAddress, message);

                Result result = ServiceUtil.SendRequest(
                    out _,
                    _sessionHandle,
                    0,
                    sendPid: false,
                    ReadOnlySpan<byte>.Empty,
                    stackalloc[] { HipcBufferFlags.In | HipcBufferFlags.AutoSelect },
                    stackalloc[] { new PointerAndSize(bufferAddress, bufferSize) });

                _allocator.Free(bufferAddress, bufferSize);

                return result;
            }

            public void Dispose()
            {
                if (_sessionHandle != 0)
                {
                    HorizonStatic.Syscall.CloseHandle(_sessionHandle);

                    _sessionHandle = 0;
                }

                GC.SuppressFinalize(this);
            }
        }

        private const string LmName = "lm";

        private readonly HeapAllocator _allocator;
        private int _sessionHandle;

        public LmApi(HeapAllocator allocator, SmApi smApi)
        {
            _allocator = allocator;

            smApi.GetServiceHandle(out _sessionHandle, ServiceName.Encode(LmName)).AbortOnFailure();
        }

        public Result OpenLogger(out Logger logger)
        {
            Result result = ServiceUtil.SendRequest(out CmifResponse response, _sessionHandle, 0, sendPid: true, ReadOnlySpan<byte>.Empty);
            if (result.IsFailure)
            {
                logger = default;

                return result;
            }

            logger = new Logger(_allocator, response.MoveHandles[0]);

            return Result.Success;
        }

        public void Dispose()
        {
            if (_sessionHandle != 0)
            {
                HorizonStatic.Syscall.CloseHandle(_sessionHandle);

                _sessionHandle = 0;
            }

            GC.SuppressFinalize(this);
        }
    }
}
