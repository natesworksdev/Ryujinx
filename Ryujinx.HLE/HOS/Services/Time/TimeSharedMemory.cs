using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Services.Time.Clock;
using Ryujinx.HLE.HOS.Services.Time.Types;
using Ryujinx.HLE.Utilities;

namespace Ryujinx.HLE.HOS.Services.Time
{
    class TimeSharedMemory
    {
        private int   _sharedMemoryHandle;
        private ulong _sharedMemoryBaseAddress;

        private const uint SharedMemorySize                 = 0x1000;
        private const uint SteadyClockContextOffset         = 0x00;
        private const uint LocalSystemClockContextOffset    = 0x38;
        private const uint NetworkSystemClockContextOffset  = 0x80;
        private const uint AutomaticCorrectionEnabledOffset = 0xC8;

        public void Initialize()
        {
            Map.LocateMappableSpace(out _sharedMemoryBaseAddress, SharedMemorySize);

            KernelStatic.Syscall.CreateSharedMemory(
                out _sharedMemoryHandle,
                SharedMemorySize,
                KMemoryPermission.ReadAndWrite,
                KMemoryPermission.Read);

            KernelStatic.Syscall.MapSharedMemory(
                _sharedMemoryHandle,
                _sharedMemoryBaseAddress,
                SharedMemorySize,
                KMemoryPermission.ReadAndWrite);
        }

        public int GetSharedMemoryHandle()
        {
            return _sharedMemoryHandle;
        }

        public void SetupStandardSteadyClock(UInt128 clockSourceId, TimeSpanType currentTimePoint)
        {
            TimeSpanType ticksTimeSpan = TimeSpanType.FromTimeSpan(ARMeilleure.State.ExecutionContext.ElapsedTime);

            SteadyClockContext context = new SteadyClockContext
            {
                InternalOffset = (ulong)(currentTimePoint.NanoSeconds - ticksTimeSpan.NanoSeconds),
                ClockSourceId  = clockSourceId
            };

            WriteObjectToSharedMemory(SteadyClockContextOffset, 4, context);
        }

        public void SetAutomaticCorrectionEnabled(bool isAutomaticCorrectionEnabled)
        {
            // We convert the bool to byte here as a bool in C# takes 4 bytes...
            WriteObjectToSharedMemory(AutomaticCorrectionEnabledOffset, 0, Convert.ToByte(isAutomaticCorrectionEnabled));
        }

        public void SetSteadyClockRawTimePoint(TimeSpanType currentTimePoint)
        {
            SteadyClockContext context       = ReadObjectFromSharedMemory<SteadyClockContext>(SteadyClockContextOffset, 4);
            TimeSpanType       ticksTimeSpan = TimeSpanType.FromTimeSpan(ARMeilleure.State.ExecutionContext.ElapsedTime);

            context.InternalOffset = (ulong)(currentTimePoint.NanoSeconds - ticksTimeSpan.NanoSeconds);

            WriteObjectToSharedMemory(SteadyClockContextOffset, 4, context);
        }

        public void UpdateLocalSystemClockContext(SystemClockContext context)
        {
            WriteObjectToSharedMemory(LocalSystemClockContextOffset, 4, context);
        }

        public void UpdateNetworkSystemClockContext(SystemClockContext context)
        {
            WriteObjectToSharedMemory(NetworkSystemClockContextOffset, 4, context);
        }

        private T ReadObjectFromSharedMemory<T>(ulong offset, ulong padding) where T : unmanaged
        {
            ulong indexOffset = _sharedMemoryBaseAddress + offset;

            T    result;
            uint index;
            uint possiblyNewIndex;

            do
            {
                index = KernelStatic.AddressSpace.Read<uint>(indexOffset);

                ulong objectOffset = indexOffset + 4 + padding + (ulong)((index & 1) * Unsafe.SizeOf<T>());

                result = KernelStatic.AddressSpace.Read<T>(objectOffset);

                Thread.MemoryBarrier();

                possiblyNewIndex = KernelStatic.AddressSpace.Read<uint>(indexOffset);
            }
            while (index != possiblyNewIndex);

            return result;
        }

        private void WriteObjectToSharedMemory<T>(ulong offset, ulong padding, T value) where T : unmanaged
        {
            ulong indexOffset  = _sharedMemoryBaseAddress + offset;
            uint  newIndex     = KernelStatic.AddressSpace.Read<uint>(indexOffset) + 1;
            ulong objectOffset = indexOffset + 4 + padding + (ulong)((newIndex & 1) * Unsafe.SizeOf<T>());

            KernelStatic.AddressSpace.Write(objectOffset, value);

            Thread.MemoryBarrier();

            KernelStatic.AddressSpace.Write(indexOffset, newIndex);
        }
    }
}
