using Ryujinx.Common;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Kernel.Common;
using Ryujinx.Horizon.Kernel.Memory;
using System.Collections.Generic;

namespace Ryujinx.Horizon.Kernel.Ipc
{
    class KBufferDescriptorTable
    {
        private const int MaxInternalBuffersCount = 8;

        private List<KBufferDescriptor> _sendBufferDescriptors;
        private List<KBufferDescriptor> _receiveBufferDescriptors;
        private List<KBufferDescriptor> _exchangeBufferDescriptors;

        public KBufferDescriptorTable()
        {
            _sendBufferDescriptors = new List<KBufferDescriptor>(MaxInternalBuffersCount);
            _receiveBufferDescriptors = new List<KBufferDescriptor>(MaxInternalBuffersCount);
            _exchangeBufferDescriptors = new List<KBufferDescriptor>(MaxInternalBuffersCount);
        }

        public Result AddSendBuffer(ulong src, ulong dst, ulong size, KMemoryState state)
        {
            return Add(_sendBufferDescriptors, src, dst, size, state);
        }

        public Result AddReceiveBuffer(ulong src, ulong dst, ulong size, KMemoryState state)
        {
            return Add(_receiveBufferDescriptors, src, dst, size, state);
        }

        public Result AddExchangeBuffer(ulong src, ulong dst, ulong size, KMemoryState state)
        {
            return Add(_exchangeBufferDescriptors, src, dst, size, state);
        }

        private Result Add(List<KBufferDescriptor> list, ulong src, ulong dst, ulong size, KMemoryState state)
        {
            if (list.Count < MaxInternalBuffersCount)
            {
                list.Add(new KBufferDescriptor(src, dst, size, state));

                return Result.Success;
            }

            return KernelResult.OutOfMemory;
        }

        public Result CopyBuffersToClient(KMemoryManager memoryManager)
        {
            Result result = CopyToClient(memoryManager, _receiveBufferDescriptors);

            if (result != Result.Success)
            {
                return result;
            }

            return CopyToClient(memoryManager, _exchangeBufferDescriptors);
        }

        private Result CopyToClient(KMemoryManager memoryManager, List<KBufferDescriptor> list)
        {
            foreach (KBufferDescriptor desc in list)
            {
                KMemoryState stateMask;

                switch (desc.State)
                {
                    case KMemoryState.IpcBuffer0: stateMask = KMemoryState.IpcSendAllowedType0; break;
                    case KMemoryState.IpcBuffer1: stateMask = KMemoryState.IpcSendAllowedType1; break;
                    case KMemoryState.IpcBuffer3: stateMask = KMemoryState.IpcSendAllowedType3; break;

                    default: return KernelResult.InvalidCombination;
                }

                KMemoryAttribute attributeMask = KMemoryAttribute.Borrowed | KMemoryAttribute.Uncached;

                if (desc.State == KMemoryState.IpcBuffer0)
                {
                    attributeMask |= KMemoryAttribute.DeviceMapped;
                }

                ulong clientAddrTruncated = BitUtils.AlignDown(desc.ClientAddress, KMemoryManager.PageSize);
                ulong clientAddrRounded = BitUtils.AlignUp(desc.ClientAddress, KMemoryManager.PageSize);

                // Check if address is not aligned, in this case we need to perform 2 copies.
                if (clientAddrTruncated != clientAddrRounded)
                {
                    ulong copySize = clientAddrRounded - desc.ClientAddress;

                    if (copySize > desc.Size)
                    {
                        copySize = desc.Size;
                    }

                    Result result = memoryManager.CopyDataFromCurrentProcess(
                        desc.ClientAddress,
                        copySize,
                        stateMask,
                        stateMask,
                        KMemoryPermission.ReadAndWrite,
                        attributeMask,
                        KMemoryAttribute.None,
                        desc.ServerAddress);

                    if (result != Result.Success)
                    {
                        return result;
                    }
                }

                ulong clientEndAddr = desc.ClientAddress + desc.Size;
                ulong serverEndAddr = desc.ServerAddress + desc.Size;

                ulong clientEndAddrTruncated = BitUtils.AlignDown(clientEndAddr, KMemoryManager.PageSize);
                ulong clientEndAddrRounded = BitUtils.AlignUp(clientEndAddr, KMemoryManager.PageSize);
                ulong serverEndAddrTruncated = BitUtils.AlignDown(serverEndAddr, KMemoryManager.PageSize);

                if (clientEndAddrTruncated < clientEndAddrRounded &&
                    (clientAddrTruncated == clientAddrRounded || clientAddrTruncated < clientEndAddrTruncated))
                {
                    Result result = memoryManager.CopyDataFromCurrentProcess(
                        clientEndAddrTruncated,
                        clientEndAddr - clientEndAddrTruncated,
                        stateMask,
                        stateMask,
                        KMemoryPermission.ReadAndWrite,
                        attributeMask,
                        KMemoryAttribute.None,
                        serverEndAddrTruncated);

                    if (result != Result.Success)
                    {
                        return result;
                    }
                }
            }

            return Result.Success;
        }

        public Result UnmapServerBuffers(KMemoryManager memoryManager)
        {
            Result result = UnmapServer(memoryManager, _sendBufferDescriptors);

            if (result != Result.Success)
            {
                return result;
            }

            result = UnmapServer(memoryManager, _receiveBufferDescriptors);

            if (result != Result.Success)
            {
                return result;
            }

            return UnmapServer(memoryManager, _exchangeBufferDescriptors);
        }

        private Result UnmapServer(KMemoryManager memoryManager, List<KBufferDescriptor> list)
        {
            foreach (KBufferDescriptor descriptor in list)
            {
                Result result = memoryManager.UnmapNoAttributeIfStateEquals(
                    descriptor.ServerAddress,
                    descriptor.Size,
                    descriptor.State);

                if (result != Result.Success)
                {
                    return result;
                }
            }

            return Result.Success;
        }

        public Result RestoreClientBuffers(KMemoryManager memoryManager)
        {
            Result result = RestoreClient(memoryManager, _sendBufferDescriptors);

            if (result != Result.Success)
            {
                return result;
            }

            result = RestoreClient(memoryManager, _receiveBufferDescriptors);

            if (result != Result.Success)
            {
                return result;
            }

            return RestoreClient(memoryManager, _exchangeBufferDescriptors);
        }

        private Result RestoreClient(KMemoryManager memoryManager, List<KBufferDescriptor> list)
        {
            foreach (KBufferDescriptor descriptor in list)
            {
                Result result = memoryManager.UnmapIpcRestorePermission(
                    descriptor.ClientAddress,
                    descriptor.Size,
                    descriptor.State);

                if (result != Result.Success)
                {
                    return result;
                }
            }

            return Result.Success;
        }
    }
}