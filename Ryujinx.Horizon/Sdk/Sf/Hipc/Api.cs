using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Common;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Hipc
{
    static class Api
    {
        public const int TlsMessageBufferSize = 0x100;

        public static Result Receive(out ReceiveResult recvResult, int sessionHandle, Span<byte> messageBuffer)
        {
            Result result = ReceiveImpl(sessionHandle, messageBuffer);

            if (result == KernelResult.PortRemoteClosed)
            {
                recvResult = ReceiveResult.Closed;
                return Result.Success;
            }
            else if (result == KernelResult.ReceiveListBroken)
            {
                recvResult = ReceiveResult.NeedsRetry;
                return Result.Success;
            }
 
            recvResult = ReceiveResult.Success;
            return result;
        }

        private static Result ReceiveImpl(int sessionHandle, Span<byte> messageBuffer)
        {
            Span<int> handles = stackalloc int[1];

            handles[0] = sessionHandle;

            var tlsSpan = KernelStatic.AddressSpace.GetSpan(KernelStatic.GetThreadContext().TlsAddress, TlsMessageBufferSize);

            if (messageBuffer == tlsSpan)
            {
                return KernelStatic.Syscall.ReplyAndReceive(handles, 0, -1L, out _);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static Result Reply(int sessionHandle, ReadOnlySpan<byte> messageBuffer)
        {
            Result result = ReplyImpl(sessionHandle, messageBuffer);

            result.AbortUnless(KernelResult.TimedOut, KernelResult.PortRemoteClosed);

            return Result.Success;
        }

        private static Result ReplyImpl(int sessionHandle, ReadOnlySpan<byte> messageBuffer)
        {
            Span<int> handles = stackalloc int[1];

            handles[0] = sessionHandle;

            var tlsSpan = KernelStatic.AddressSpace.GetSpan(KernelStatic.GetThreadContext().TlsAddress, TlsMessageBufferSize);

            if (messageBuffer == tlsSpan)
            {
                return KernelStatic.Syscall.ReplyAndReceive(handles, sessionHandle, 0, out _);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static Result CreateSession(out int serverHandle, out int clientHandle)
        {
            Result result = KernelStatic.Syscall.CreateSession(false, 0UL, out serverHandle, out clientHandle);

            if (result == KernelResult.OutOfResource)
            {
                return HipcResult.OutOfSessions;
            }

            return result;
        }
    }
}
