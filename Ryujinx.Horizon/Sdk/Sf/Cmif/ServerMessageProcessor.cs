using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    abstract class ServerMessageProcessor
    {
        public abstract void SetImplementationProcessor(ServerMessageProcessor impl);
        public abstract ServerMessageRuntimeMetadata GetRuntimeMetadata();

        public abstract Result PrepareForProcess(ref ServiceDispatchContext context, ServerMessageRuntimeMetadata runtimeMetadata);
        public abstract Result GetInObjects(Span<ServiceObjectHolder> inObjects);
        public abstract HipcMessageData PrepareForReply(ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata);
        public abstract void PrepareForErrorReply(ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata);
        public abstract void SetOutObjects(ref ServiceDispatchContext context, HipcMessageData response, Span<ServiceObjectHolder> outObjects);
    }
}
