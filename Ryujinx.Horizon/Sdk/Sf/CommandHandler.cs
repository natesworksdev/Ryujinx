using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Sf.Cmif;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    class CommandHandler
    {
        private readonly MethodInfo _method;
        private readonly IServiceObject _instance;
        private readonly HipcCommandProcessor _processor;

        public CommandHandler(MethodInfo method, IServiceObject instance)
        {
            _method = method;
            _instance = instance;
            _processor = new HipcCommandProcessor(method.GetParameters());
        }

        public Result Invoke(ref Span<CmifOutHeader> outHeader, ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData)
        {
            if (context.Processor == null)
            {
                context.Processor = _processor;
            }
            else
            {
                context.Processor.SetImplementationProcessor(_processor);
            }

            var runtimeMetadata = context.Processor.GetRuntimeMetadata();
            Result result = context.Processor.PrepareForProcess(ref context, runtimeMetadata);

            if (result.IsFailure)
            {
                return result;
            }

            object[] args = new object[_processor.FunctionArgumentsCount];
            bool[] isBufferMapAlias = new bool[_processor.FunctionArgumentsCount];

            result = _processor.ProcessBuffers(ref context, args, isBufferMapAlias, runtimeMetadata);

            if (result.IsFailure)
            {
                return result;
            }

            result = _processor.GetInObjects(context.Processor, args);

            if (result.IsFailure)
            {
                return result;
            }

            _processor.DeserializeArguments(ref context, inRawData, args);

            Span<byte> outRawData;

            if (_method.ReturnType == typeof(Result))
            {
                Result commandResult = (Result)_method.Invoke(_instance, args);

                if (commandResult.IsFailure)
                {
                    context.Processor.PrepareForErrorReply(ref context, out outRawData, runtimeMetadata);
                    GetCmifOutHeaderPointer(ref outHeader, ref outRawData);
                    return commandResult;
                }
            }
            else
            {
                _method.Invoke(_instance, args);
            }

            var response = context.Processor.PrepareForReply(ref context, out outRawData, runtimeMetadata);
            GetCmifOutHeaderPointer(ref outHeader, ref outRawData);

            if (outRawData.Length < _processor.OutRawDataSize)
            {
                return SfResult.InvalidOutRawSize;
            }

            _processor.SerializeArguments(response, outRawData, args, runtimeMetadata.OutObjectsCount);
            _processor.SetOutBuffers(response, args, isBufferMapAlias);
            _processor.SetOutObjects(ref context, response, args);

            return Result.Success;
        }

        private static void GetCmifOutHeaderPointer(ref Span<CmifOutHeader> outHeader, ref Span<byte> outRawData)
        {
            outHeader = MemoryMarshal.Cast<byte, CmifOutHeader>(outRawData).Slice(0, 1);
            outRawData = outRawData.Slice(Unsafe.SizeOf<CmifOutHeader>());
        }
    }
}
