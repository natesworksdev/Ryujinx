using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Ryujinx.Horizon.Sdk.Sf.CommandSerialization;

namespace Ryujinx.Horizon.Sdk.Sf
{
    class HipcCommandProcessor : ServerMessageProcessor
    {
        private delegate object CommandDeserialize(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset);
        private delegate object CommandCreateBuffer(ulong address, ulong size);
        private delegate void CommandSerialize(HipcMessageData response, Span<byte> outRawData, int offset, object value);

        private enum CommandArgType : byte
        {
            Invalid,

            Buffer,
            InArgument,
            InCopyHandle,
            InMoveHandle,
            InObject,
            OutArgument,
            OutCopyHandle,
            OutMoveHandle,
            OutObject,
            ProcessId
        }

        private struct CommandArg
        {
            public CommandArgType Type { get; }
            public HipcBufferFlags BufferFlags { get; }
            public ushort BufferFixedSize { get; }
            public Type ArgType { get; }
            public CommandDeserialize Deserialize { get; }
            public CommandCreateBuffer CreateBuffer { get; }
            public CommandSerialize Serialize { get; }

            public CommandArg(ParameterInfo info, CommandArgType type)
            {
                Type = type;

                Type argType = info.ParameterType;

                if (type == CommandArgType.Buffer)
                {
                    bool isIn = IsInBuffer(info);

                    HipcBufferFlags inOutFlags = isIn ? HipcBufferFlags.In : HipcBufferFlags.Out;

                    var attribute = info.GetCustomAttribute<BufferAttribute>();
                    if (attribute != null)
                    {
                        BufferFlags = (attribute.Flags & ~(HipcBufferFlags.In | HipcBufferFlags.Out)) | inOutFlags;
                        BufferFixedSize = attribute.FixedSize;
                    }
                    else
                    {
                        BufferFlags = HipcBufferFlags.HipcMapAlias | inOutFlags;
                        BufferFixedSize = 0;
                    }

                    CreateBuffer = CreateCreateBufferGeneric(isIn ? nameof(CreateInBuffer) : nameof(CreateOutBuffer), argType.GetGenericArguments()[0]);
                }
                else
                {
                    BufferFlags = 0;
                    BufferFixedSize = 0;
                    CreateBuffer = null;
                }

                Deserialize = type switch
                {
                    CommandArgType.InArgument => CreateDeserializeGeneric(nameof(DeserializeArg), GetInnerType(argType)),
                    CommandArgType.InCopyHandle => DeserializeCopyHandle,
                    CommandArgType.InMoveHandle => DeserializeMoveHandle,
                    CommandArgType.ProcessId => DeserializeClientProcessId,
                    _ => null
                };

                Serialize = type switch
                {
                    CommandArgType.OutArgument => CreateSerializeGeneric(nameof(SerializeArg), GetInnerType(argType)),
                    CommandArgType.OutCopyHandle => SerializeCopyHandle,
                    CommandArgType.OutMoveHandle => SerializeMoveHandle,
                    _ => null
                };

                ArgType = argType;
            }

            private static CommandDeserialize CreateDeserializeGeneric(string name, Type type)
            {
                return GenericMethod.CreateDelegate<CommandDeserialize>(typeof(CommandSerialization).GetMethod(name), type);
            }

            private static CommandCreateBuffer CreateCreateBufferGeneric(string name, Type type)
            {
                return GenericMethod.CreateDelegate<CommandCreateBuffer>(typeof(CommandSerialization).GetMethod(name), type);
            }

            private static CommandSerialize CreateSerializeGeneric(string name, Type type)
            {
                return GenericMethod.CreateDelegate<CommandSerialize>(typeof(CommandSerialization).GetMethod(name), type);
            }
        }

        private readonly CommandArg[] _args;

        private readonly int[] _inOffsets;
        private readonly int[] _outOffsets;

        private readonly bool _hasInProcessIdHolder;
        private readonly int _inObjectsCount;
        private readonly int _outObjectsCount;
        private readonly int _inMapAliasBuffersCount;
        private readonly int _outMapAliasBuffersCount;
        private readonly int _inPointerBuffersCount;
        private readonly int _outPointerBuffersCount;
        private readonly int _outFixedSizePointerBuffersCount;
        private readonly int _inMoveHandlesCount;
        private readonly int _inCopyHandlesCount;
        private readonly int _outMoveHandlesCount;
        private readonly int _outCopyHandlesCount;

        public int FunctionArgumentsCount => _args.Length;

        public int InRawDataSize => BitUtils.AlignUp(_inOffsets[^1], sizeof(ushort));
        public int OutRawDataSize => BitUtils.AlignUp(_outOffsets[^1], sizeof(uint));

        private int OutUnfixedSizePointerBuffersCount => _outPointerBuffersCount - _outFixedSizePointerBuffersCount;

        public HipcCommandProcessor(ParameterInfo[] args)
        {
            _args = new CommandArg[args.Length];

            for (int i = 0; i < args.Length; i++)
            {
                var argInfo = args[i];

                CommandArgType type = CommandArgType.Invalid;

                if (IsIn(argInfo))
                {
                    if (IsArgument(argInfo))
                    {
                        type = CommandArgType.InArgument;
                    }
                    else if (IsBuffer(argInfo))
                    {
                        type = CommandArgType.Buffer;

                        var flags = argInfo.GetCustomAttribute<BufferAttribute>()?.Flags ?? 0;

                        if (IsInBuffer(argInfo))
                        {
                            if (flags.HasFlag(HipcBufferFlags.HipcAutoSelect))
                            {
                                _inMapAliasBuffersCount++;
                                _inPointerBuffersCount++;
                            }
                            else if (flags.HasFlag(HipcBufferFlags.HipcMapAlias))
                            {
                                _inMapAliasBuffersCount++;
                            }
                            else if (flags.HasFlag(HipcBufferFlags.HipcPointer))
                            {
                                _inPointerBuffersCount++;
                            }
                        }
                        else
                        {
                            bool autoSelect = flags.HasFlag(HipcBufferFlags.HipcAutoSelect);
                            if (autoSelect || flags.HasFlag(HipcBufferFlags.HipcPointer))
                            {
                                _outPointerBuffersCount++;

                                if (flags.HasFlag(HipcBufferFlags.FixedSize))
                                {
                                    _outFixedSizePointerBuffersCount++;
                                }
                            }

                            if (autoSelect || flags.HasFlag(HipcBufferFlags.HipcMapAlias))
                            {
                                _outMapAliasBuffersCount++;
                            }
                        }
                    }
                    else if (IsCopyHandle(argInfo))
                    {
                        type = CommandArgType.InCopyHandle;
                        _inCopyHandlesCount++;
                    }
                    else if (IsMoveHandle(argInfo))
                    {
                        type = CommandArgType.InMoveHandle;
                        _inMoveHandlesCount++;
                    }
                    else if (IsObject(argInfo))
                    {
                        type = CommandArgType.InObject;
                        _inObjectsCount++;
                    }
                    else if (IsProcessId(argInfo))
                    {
                        type = CommandArgType.ProcessId;
                        _hasInProcessIdHolder = true;
                    }
                }
                else if (IsOut(argInfo))
                {
                    if (IsArgument(argInfo))
                    {
                        type = CommandArgType.OutArgument;
                    }
                    else if (IsCopyHandle(argInfo))
                    {
                        type = CommandArgType.OutCopyHandle;
                        _outCopyHandlesCount++;
                    }
                    else if (IsMoveHandle(argInfo))
                    {
                        type = CommandArgType.OutMoveHandle;
                        _outMoveHandlesCount++;
                    }
                    else if (IsObject(argInfo))
                    {
                        type = CommandArgType.OutObject;
                        _outObjectsCount++;
                    }
                }

                if (type == CommandArgType.Invalid)
                {
                    throw new InvalidOperationException($"Command function has a invalid parameter type \"{argInfo.ParameterType}\".");
                }

                _args[i] = new CommandArg(argInfo, type);
            }

            _inOffsets = RawDataOffsetCalculator.Calculate(args.Where(IsInArgument).Select(x => x.ParameterType).ToArray());
            _outOffsets = RawDataOffsetCalculator.Calculate(args.Where(IsOutArgument).Select(x => x.ParameterType).ToArray());
        }

        public Result ProcessBuffers(ref ServiceDispatchContext context, object[] args, bool[] isBufferMapAlias, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            bool mapAliasBuffersValid = true;

            ulong pointerBufferTail = context.PointerBuffer.Address;
            ulong pointerBufferHead = pointerBufferTail + context.PointerBuffer.Size;

            int sendMapAliasIndex = 0;
            int recvMapAliasIndex = 0;
            int sendPointerIndex = 0;
            int unfixedRecvPointerIndex = 0;

            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i].Type != CommandArgType.Buffer)
                {
                    continue;
                }

                var flags = _args[i].BufferFlags;
                bool isMapAlias;

                if (flags.HasFlag(HipcBufferFlags.HipcMapAlias))
                {
                    isMapAlias = true;
                }
                else if (flags.HasFlag(HipcBufferFlags.HipcPointer))
                {
                    isMapAlias = false;
                }
                else /* if (flags.HasFlag(HipcBufferFlags.HipcAutoSelect)) */
                {
                    var descriptor = flags.HasFlag(HipcBufferFlags.In)
                        ? context.Request.Data.SendBuffers[sendMapAliasIndex]
                        : context.Request.Data.ReceiveBuffers[recvMapAliasIndex];

                    isMapAlias = descriptor.Address != 0UL;
                }

                isBufferMapAlias[i] = isMapAlias;

                if (isMapAlias)
                {
                    var descriptor = flags.HasFlag(HipcBufferFlags.In)
                        ? context.Request.Data.SendBuffers[sendMapAliasIndex++]
                        : context.Request.Data.ReceiveBuffers[recvMapAliasIndex++];

                    args[i] = _args[i].CreateBuffer(descriptor.Address, descriptor.Size);

                    if (!IsMapTransferModeValid(flags, descriptor.Mode))
                    {
                        mapAliasBuffersValid = false;
                    }
                }
                else
                {
                    if (flags.HasFlag(HipcBufferFlags.In))
                    {
                        var descriptor = context.Request.Data.SendStatics[sendPointerIndex++];
                        ulong address = descriptor.Address;
                        ulong size = descriptor.Size;
                        args[i] = _args[i].CreateBuffer(address, size);

                        if (size != 0)
                        {
                            pointerBufferTail = Math.Max(pointerBufferTail, address + size);
                        }
                    }
                    else /* if (flags.HasFlag(HipcBufferFlags.Out)) */
                    {
                        ulong size;

                        if (flags.HasFlag(HipcBufferFlags.FixedSize))
                        {
                            size = _args[i].BufferFixedSize;
                        }
                        else
                        {
                            var data = MemoryMarshal.Cast<uint, byte>(context.Request.Data.DataWords);
                            var recvPointerSizes = MemoryMarshal.Cast<byte, ushort>(data.Slice(runtimeMetadata.UnfixedOutPointerSizeOffset));
                            size = recvPointerSizes[unfixedRecvPointerIndex++];
                        }

                        pointerBufferHead = BitUtils.AlignDown(pointerBufferHead - size, 0x10);
                        args[i] = _args[i].CreateBuffer(pointerBufferHead, size);
                    }
                }
            }

            if (!mapAliasBuffersValid)
            {
                return HipcResult.InvalidCmifRequest;
            }

            if (_outPointerBuffersCount != 0 && pointerBufferTail > pointerBufferHead)
            {
                return HipcResult.PointerBufferTooSmall;
            }

            return Result.Success;
        }

        private static bool IsMapTransferModeValid(HipcBufferFlags flags, HipcBufferMode mode)
        {
            if (flags.HasFlag(HipcBufferFlags.HipcMapTransferAllowsNonSecure))
            {
                return mode == HipcBufferMode.NonSecure;
            }
            else if (flags.HasFlag(HipcBufferFlags.HipcMapTransferAllowsNonDevice))
            {
                return mode == HipcBufferMode.NonDevice;
            }
            else
            {
                return mode == HipcBufferMode.Normal;
            }
        }

        public void SetOutBuffers(HipcMessageData response, object[] args, bool[] isBufferMapAlias)
        {
            int recvPointerIndex = 0;

            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i].Type != CommandArgType.Buffer)
                {
                    continue;
                }

                var flags = _args[i].BufferFlags;
                if (flags.HasFlag(HipcBufferFlags.Out))
                {
                    var buffer = (IBuffer)args[i];

                    if (flags.HasFlag(HipcBufferFlags.HipcPointer))
                    {
                        response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(buffer.Address, (ushort)buffer.Size, recvPointerIndex);
                    }
                    else if (flags.HasFlag(HipcBufferFlags.HipcAutoSelect))
                    {
                        if (!isBufferMapAlias[i])
                        {
                            response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(buffer.Address, (ushort)buffer.Size, recvPointerIndex);
                        }
                        else
                        {
                            response.SendStatics[recvPointerIndex] = new HipcStaticDescriptor(0UL, 0, recvPointerIndex);
                        }
                    }

                    recvPointerIndex++;

                    // Make sure that the data is written back to memory (dispose flushes the data).
                    ((IDisposable)buffer).Dispose();
                }
            }
        }

        public void DeserializeArguments(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, object[] args)
        {
            int argIndex = 0;
            int copyHandleIndex = 0;
            int moveHandleIndex = 0;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = _args[i];

                if (arg.Deserialize != null)
                {
                    int offset = arg.Type switch
                    {
                        CommandArgType.InArgument => _inOffsets[argIndex++],
                        CommandArgType.InCopyHandle => copyHandleIndex++,
                        CommandArgType.InMoveHandle => moveHandleIndex++,
                        _ => 0
                    };

                    args[i] = arg.Deserialize(ref context, inRawData, offset);
                }
            }
        }

        public void SerializeArguments(HipcMessageData response, Span<byte> outRawData, object[] args, int outObjectsCount)
        {
            int argIndex = 0;
            int copyHandleIndex = 0;
            int moveHandleIndex = outObjectsCount;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = _args[i];

                if (arg.Serialize != null)
                {
                    int offset = arg.Type switch
                    {
                        CommandArgType.OutArgument => _outOffsets[argIndex++],
                        CommandArgType.OutCopyHandle => copyHandleIndex++,
                        CommandArgType.OutMoveHandle => moveHandleIndex++,
                        _ => 0
                    };

                    arg.Serialize(response, outRawData, offset, args[i]);
                }
            }
        }

        public override void SetImplementationProcessor(ServerMessageProcessor impl)
        {
            // We don't need to do anything here as this should be always the last processor to be called.
        }

        public override ServerMessageRuntimeMetadata GetRuntimeMetadata()
        {
            return new ServerMessageRuntimeMetadata(
                (ushort)InRawDataSize,
                (ushort)OutRawDataSize,
                (byte)Unsafe.SizeOf<CmifInHeader>(),
                (byte)Unsafe.SizeOf<CmifOutHeader>(),
                (byte)_inObjectsCount,
                (byte)_outObjectsCount);
        }

        public override Result PrepareForProcess(ref ServiceDispatchContext context, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            ref var meta = ref context.Request.Meta;
            bool requestValid = true;
            requestValid &= meta.SendPid == _hasInProcessIdHolder;
            requestValid &= meta.SendStaticsCount == _inPointerBuffersCount;
            requestValid &= meta.SendBuffersCount == _inMapAliasBuffersCount;
            requestValid &= meta.ReceiveBuffersCount == _outMapAliasBuffersCount;
            requestValid &= meta.ExchangeBuffersCount == 0;
            requestValid &= meta.CopyHandlesCount == _inCopyHandlesCount;
            requestValid &= meta.MoveHandlesCount == _inMoveHandlesCount;

            int rawSizeInBytes = meta.DataWordsCount * sizeof(uint);
            int commandRawSize = BitUtils.AlignUp(runtimeMetadata.UnfixedOutPointerSizeOffset + (OutUnfixedSizePointerBuffersCount * sizeof(ushort)), sizeof(uint));
            requestValid &= rawSizeInBytes >= commandRawSize;

            return requestValid ? Result.Success : HipcResult.InvalidCmifRequest;
        }

        public Result GetInObjects(ServerMessageProcessor processor, object[] args)
        {
            if (_inObjectsCount == 0)
            {
                return Result.Success;
            }

            ServiceObjectHolder[] inObjects = new ServiceObjectHolder[_inObjectsCount];
            Result result = processor.GetInObjects(inObjects);

            if (result.IsFailure)
            {
                return result;
            }

            int inObjectIndex = 0;

            for (int i = 0; i < args.Length; i++)
            {
                if (_args[i].Type == CommandArgType.InObject)
                {
                    var inObject = inObjects[inObjectIndex++];
                    if (inObject == null)
                    {
                        continue;
                    }

                    if (inObject.ServiceObject.GetType() != _args[i].ArgType)
                    {
                        return SfResult.InvalidInObject;
                    }

                    args[i] = inObject.ServiceObject;
                }
            }

            return Result.Success;
        }

        public override Result GetInObjects(Span<ServiceObjectHolder> inObjects)
        {
            return SfResult.NotSupported;
        }

        public override HipcMessageData PrepareForReply(ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            int rawDataSize = OutRawDataSize + runtimeMetadata.OutHeadersSize;
            var response = HipcMessage.WriteResponse(
                context.OutMessageBuffer,
                _outPointerBuffersCount,
                (BitUtils.AlignUp(rawDataSize, 4) + 0x10) / sizeof(uint),
                _outCopyHandlesCount,
                _outMoveHandlesCount + runtimeMetadata.OutObjectsCount);
            outRawData = MemoryMarshal.Cast<uint, byte>(response.DataWords);
            return response;
        }

        public override void PrepareForErrorReply(ref ServiceDispatchContext context, out Span<byte> outRawData, ServerMessageRuntimeMetadata runtimeMetadata)
        {
            int rawDataSize = runtimeMetadata.OutHeadersSize;
            var response = HipcMessage.WriteResponse(
                context.OutMessageBuffer,
                0,
                (BitUtils.AlignUp(rawDataSize, 4) + 0x10) / sizeof(uint),
                0,
                0);
            outRawData = MemoryMarshal.Cast<uint, byte>(response.DataWords);
        }

        public void SetOutObjects(ref ServiceDispatchContext context, HipcMessageData response, object[] args)
        {
            if (_outObjectsCount == 0)
            {
                return;
            }

            ServiceObjectHolder[] outObjects = new ServiceObjectHolder[_outObjectsCount];
            int outObjectIndex = 0;

            for (int i = 0; i < args.Length; i++)
            {
                if (_args[i].Type == CommandArgType.OutObject)
                {
                    outObjects[outObjectIndex++] = args[i] != null ? new ServiceObjectHolder((IServiceObject)args[i]) : null;
                }
            }

            context.Processor.SetOutObjects(ref context, response, outObjects);
        }

        public override void SetOutObjects(ref ServiceDispatchContext context, HipcMessageData response, Span<ServiceObjectHolder> outObjects)
        {
            for (int index = 0; index < _outObjectsCount; index++)
            {
                SetOutObjectImpl(index, response, context.Manager, outObjects[index]);
            }
        }

        private void SetOutObjectImpl(int index, HipcMessageData response, ServerSessionManager manager, ServiceObjectHolder obj)
        {
            if (obj == null)
            {
                response.MoveHandles[index] = 0;
                return;
            }

            Api.CreateSession(out int serverHandle, out int clientHandle).AbortOnFailure();
            manager.RegisterSession(serverHandle, obj).AbortOnFailure();
            response.MoveHandles[index] = clientHandle;
        }
    }
}
