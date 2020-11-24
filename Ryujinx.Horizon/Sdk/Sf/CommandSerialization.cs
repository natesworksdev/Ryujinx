using Ryujinx.Horizon.Sdk.Sf.Cmif;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Horizon.Sdk.Sf
{
    static class CommandSerialization
    {
        public static object CreateInBuffer<T>(ulong address, ulong size) where T : unmanaged
        {
            return new InBuffer<T>(address, size);
        }

        public static object CreateOutBuffer<T>(ulong address, ulong size) where T : unmanaged
        {
            return new OutBuffer<T>(address, size);
        }

        public static object DeserializeArg<T>(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset) where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(inRawData.Slice(offset, Unsafe.SizeOf<T>()))[0];
        }

        public static object DeserializeClientProcessId(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset)
        {
            return context.Request.Pid;
        }

        public static object DeserializeCopyHandle(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset)
        {
            return context.Request.Data.CopyHandles[offset];
        }

        public static object DeserializeMoveHandle(ref ServiceDispatchContext context, ReadOnlySpan<byte> inRawData, int offset)
        {
            return context.Request.Data.MoveHandles[offset];
        }

        public static void SerializeArg<T>(HipcMessageData response, Span<byte> outRawData, int offset, object value) where T : unmanaged
        {
            MemoryMarshal.Cast<byte, T>(outRawData.Slice(offset, Unsafe.SizeOf<T>()))[0] = (T)value;
        }

        public static void SerializeCopyHandle(HipcMessageData response, Span<byte> outRawData, int offset, object value)
        {
            response.CopyHandles[offset] = (int)value;
        }

        public static void SerializeMoveHandle(HipcMessageData response, Span<byte> outRawData, int offset, object value)
        {
            response.MoveHandles[offset] = (int)value;
        }

        public static bool IsInArgument(ParameterInfo info)
        {
            return IsIn(info) && IsArgument(info);
        }

        public static bool IsOutArgument(ParameterInfo info)
        {
            return IsOut(info) && IsArgument(info);
        }

        public static bool IsArgument(ParameterInfo info)
        {
            return !IsBuffer(info) && !IsHandle(info) && !IsObject(info) && !IsProcessId(info);
        }

        public static bool IsBuffer(ParameterInfo info)
        {
            return IsInBuffer(info) || IsOutBuffer(info);
        }

        public static bool IsInBuffer(ParameterInfo info)
        {
            return info.ParameterType.GetGenericArguments().Length == 1 &&
                   info.ParameterType.GetGenericTypeDefinition() == typeof(InBuffer<>);
        }

        public static bool IsOutBuffer(ParameterInfo info)
        {
            return info.ParameterType.GetGenericArguments().Length == 1 &&
                   info.ParameterType.GetGenericTypeDefinition() == typeof(OutBuffer<>);
        }

        public static bool IsHandle(ParameterInfo info)
        {
            return IsCopyHandle(info) || IsMoveHandle(info);
        }

        public static bool IsCopyHandle(ParameterInfo info)
        {
            return HasAttribute<CopyHandleAttribute>(info);
        }

        public static bool IsMoveHandle(ParameterInfo info)
        {
            return HasAttribute<MoveHandleAttribute>(info);
        }

        public static bool IsObject(ParameterInfo info)
        {
            return typeof(IServiceObject).IsAssignableFrom(GetInnerType(info.ParameterType));
        }

        public static bool IsProcessId(ParameterInfo info)
        {
            return HasAttribute<ClientProcessIdAttribute>(info);
        }

        public static bool IsIn(ParameterInfo info)
        {
            return !IsOut(info);
        }

        public static bool IsOut(ParameterInfo info)
        {
            return info.IsOut;
        }

        public static bool HasAttribute<T>(ParameterInfo info) where T : Attribute
        {
            return info.GetCustomAttribute<T>() != null;
        }

        public static Type GetInnerType(Type type)
        {
            if (type.IsByRef)
            {
                type = type.GetElementType();
            }

            return type;
        }
    }
}
