using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Kernel.Process;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices
{
    abstract class NvFileDevice
    {
        protected KProcess _owner;

        public NvFileDevice(ServiceCtx context)
        {
            _owner = context.Process;
        }

        public KProcess GetOwner()
        {
            return _owner;
        }

        public virtual NvInternalResult QueryEvent(out int eventHandle, uint eventId)
        {
            eventHandle = 0;

            return NvInternalResult.NotImplemented;
        }

        public virtual NvInternalResult MapSharedMemory(KSharedMemory sharedMemory, uint argument)
        {
            return NvInternalResult.NotImplemented;
        }

        public virtual NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            return NvInternalResult.NotImplemented;
        }

        public virtual NvInternalResult Ioctl2(NvIoctl command, Span<byte> arguments, Span<byte> inlineInBuffer)
        {
            return NvInternalResult.NotImplemented;
        }

        public virtual NvInternalResult Ioctl3(NvIoctl command, Span<byte> arguments, Span<byte> inlineOutBuffer)
        {
            return NvInternalResult.NotImplemented;
        }

        protected delegate NvInternalResult IoctlProcessor<T>(ref T arguments);
        protected delegate NvInternalResult IoctlProcessorSpan<T>(Span<T> arguments);
        protected delegate NvInternalResult IoctlProcessorInline<T, Y>(ref T arguments, Span<Y> inlineData);

        protected static NvInternalResult CallIoctlMethod<T>(IoctlProcessor<T> callback, Span<byte> arguments) where T : struct
        {
            Debug.Assert(arguments.Length == Unsafe.SizeOf<T>());
            return callback(ref MemoryMarshal.Cast<byte, T>(arguments)[0]);
        }

        protected static NvInternalResult CallIoctlMethod<T, Y>(IoctlProcessorInline<T, Y> callback, Span<byte> arguments, Span<byte> inlineInBuffer) where T : struct where Y : struct
        {
            Debug.Assert(arguments.Length == Unsafe.SizeOf<T>());
            return callback(ref MemoryMarshal.Cast<byte, T>(arguments)[0], MemoryMarshal.Cast<byte, Y>(inlineInBuffer));
        }

        protected static NvInternalResult CallIoctlMethod<T>(IoctlProcessorSpan<T> callback, Span<byte> arguments) where T : struct
        {
            return callback(MemoryMarshal.Cast<byte, T>(arguments));
        }

        public abstract void Close();
    }
}
