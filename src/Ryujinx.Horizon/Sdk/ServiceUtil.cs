using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Threading.Tasks;

namespace Ryujinx.Horizon.Sdk
{
    static class ServiceUtil
    {
        public static Result SendRequest(out CmifResponse response, int sessionHandle, uint requestId, bool sendPid, scoped ReadOnlySpan<byte> data)
        {
            ulong tlsAddress = HorizonStatic.ThreadContext.TlsAddress;
            int   tlsSize    = Api.TlsMessageBufferSize;

            using (var tlsRegion = HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize))
            {
                CmifRequest request = CmifMessage.CreateRequest(tlsRegion.Memory.Span, new CmifRequestFormat()
                {
                    DataSize  = data.Length,
                    RequestId = requestId,
                    SendPid   = sendPid
                });

                data.CopyTo(request.Data);
            }

            // TODO: revisit maybe propagate async ?
            Logger.Info?.Print(LogClass.Kernel, "pre-send-sync");
            // WARNING: async => sync
            // var result = HorizonStatic.Syscall.SendSyncRequest(sessionHandle).GetAwaiter().GetResult();
            var result = Task.Run(async () => await HorizonStatic.Syscall.SendSyncRequest(sessionHandle)).GetAwaiter().GetResult();
            // var result = HorizonStatic.Syscall.SendSyncRequest(sessionHandle).ConfigureAwait(false).GetAwaiter().GetResult();

            if (result.IsFailure)
            {
                Logger.Info?.Print(LogClass.Kernel, $"SendRequest failed: {result}");
                response = default;

                return result;
            }

            result = CmifMessage.ParseResponse(out response, HorizonStatic.AddressSpace.GetWritableRegion(tlsAddress, tlsSize).Memory.Span, false, 0);
            Logger.Info?.Print(LogClass.Kernel, $"SendRequest succeeded, ParseResponse: {result}");
            return result;
        }
    }
}
