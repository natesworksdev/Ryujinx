using Ryujinx.Graphics.GAL;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;
using VkBuffer = Silk.NET.Vulkan.Buffer;
using VkFormat = Silk.NET.Vulkan.Format;

namespace Ryujinx.Graphics.Vulkan
{
    class BufferHolder : IDisposable
    {
        private const int MaxUpdateBufferSize = 0x10000;

        public const AccessFlags DefaultAccessFlags =
            AccessFlags.AccessShaderReadBit |
            AccessFlags.AccessShaderWriteBit |
            AccessFlags.AccessTransferReadBit |
            AccessFlags.AccessTransferWriteBit |
            AccessFlags.AccessUniformReadBit |
            AccessFlags.AccessShaderReadBit |
            AccessFlags.AccessShaderWriteBit;

        private readonly VulkanGraphicsDevice _gd;
        private readonly Device _device;
        private readonly MemoryAllocation _allocation;
        private readonly Auto<DisposableBuffer> _buffer;
        private readonly Auto<MemoryAllocation> _allocationAuto;
        private readonly ulong _bufferHandle;

        private CacheByRange<BufferHolder> _cachedConvertedIndexBuffers;

        public int Size { get; }

        private IntPtr _map;

        private readonly MultiFenceHolder _waitable;

        private bool _lastAccessIsWrite;

        public BufferHolder(VulkanGraphicsDevice gd, Device device, VkBuffer buffer, MemoryAllocation allocation, int size)
        {
            _gd = gd;
            _device = device;
            _allocation = allocation;
            _allocationAuto = new Auto<MemoryAllocation>(allocation);
            _waitable = new MultiFenceHolder();
            _buffer = new Auto<DisposableBuffer>(new DisposableBuffer(gd.Api, device, buffer), _waitable, _allocationAuto);
            _bufferHandle = buffer.Handle;
            Size = size;
            _map = allocation.HostPointer;
        }

        public unsafe Auto<DisposableBufferView> CreateView(VkFormat format, int offset, int size)
        {
            var bufferViewCreateInfo = new BufferViewCreateInfo()
            {
                SType = StructureType.BufferViewCreateInfo,
                Buffer = new VkBuffer(_bufferHandle),
                Format = format,
                Offset = (uint)offset,
                Range = (uint)size
            };

            _gd.Api.CreateBufferView(_device, bufferViewCreateInfo, null, out var bufferView).ThrowOnError();

            return new Auto<DisposableBufferView>(new DisposableBufferView(_gd.Api, _device, bufferView), _waitable, _buffer);
        }

        public unsafe void InsertBarrier(CommandBuffer commandBuffer, bool isWrite)
        {
            // If the last access is write, we always need a barrier to be sure we will read or modify
            // the correct data.
            // If the last access is read, and current one is a write, we need to wait until the
            // read finishes to avoid overwriting data still in use.
            // Otherwise, if the last access is a read and the current one too, we don't need barriers.
            bool needsBarrier = isWrite || _lastAccessIsWrite;

            _lastAccessIsWrite = isWrite;

            if (needsBarrier)
            {
                MemoryBarrier memoryBarrier = new MemoryBarrier()
                {
                    SType = StructureType.MemoryBarrier,
                    SrcAccessMask = DefaultAccessFlags,
                    DstAccessMask = DefaultAccessFlags
                };

                _gd.Api.CmdPipelineBarrier(
                    commandBuffer,
                    PipelineStageFlags.PipelineStageAllCommandsBit,
                    PipelineStageFlags.PipelineStageAllCommandsBit,
                    DependencyFlags.DependencyDeviceGroupBit,
                    1,
                    memoryBarrier,
                    0,
                    null,
                    0,
                    null);
            }
        }

        public Auto<DisposableBuffer> GetBuffer()
        {
            return _buffer;
        }

        public Auto<DisposableBuffer> GetBuffer(CommandBuffer commandBuffer, bool isWrite = false)
        {
            if (isWrite)
            {
                _cachedConvertedIndexBuffers.Clear();
            }

            // InsertBarrier(commandBuffer, isWrite);
            return _buffer;
        }

        public BufferHandle GetHandle()
        {
            var handle = _bufferHandle;
            return Unsafe.As<ulong, BufferHandle>(ref handle);
        }

        public unsafe IntPtr Map(int offset, int mappingSize)
        {
            return _map;
        }

        public unsafe ReadOnlySpan<byte> GetData(int offset, int size)
        {
            return GetDataStorage(offset, size);
        }

        public unsafe Span<byte> GetDataStorage(int offset, int size)
        {
            int mappingSize = Math.Min(size, Size - offset);

            if (_map != IntPtr.Zero)
            {
                return new Span<byte>((void*)(_map + offset), mappingSize);
            }

            throw new InvalidOperationException("The buffer is not host mapped.");
        }

        public unsafe void SetData(int offset, ReadOnlySpan<byte> data, CommandBufferScoped? cbs = null, Action endRenderPass = null)
        {
            int dataSize = Math.Min(data.Length, Size - offset);
            if (dataSize == 0)
            {
                return;
            }

            bool needsFlush = _gd.CommandBufferPool.HasWaitableOnRentedCommandBuffer(_waitable, offset, dataSize);
            bool needsWait = needsFlush || MayWait(offset, dataSize);

            if (cbs == null ||
                !needsWait ||
                !VulkanConfiguration.UseFastBufferUpdates ||
                !TryPushData(cbs.Value, endRenderPass, offset, data))
            {
                // Some pending command might access the buffer,
                // so we need to ensure they are all submited to the GPU before waiting.
                // The flush below forces the submission of all pending commands.
                if (needsFlush)
                {
                    _gd.FlushAllCommands();
                }

                WaitForFences(offset, dataSize);

                if (_map != IntPtr.Zero)
                {
                    data.Slice(0, dataSize).CopyTo(new Span<byte>((void*)(_map + offset), dataSize));
                }
            }
        }

        public unsafe void SetDataUnchecked(int offset, ReadOnlySpan<byte> data)
        {
            int dataSize = Math.Min(data.Length, Size - offset);
            if (dataSize == 0)
            {
                return;
            }

            if (_map != IntPtr.Zero)
            {
                data.Slice(0, dataSize).CopyTo(new Span<byte>((void*)(_map + offset), dataSize));
            }
        }

        public void SetDataInline(CommandBufferScoped cbs, Action endRenderPass, int dstOffset, ReadOnlySpan<byte> data)
        {
            if (!TryPushData(cbs, endRenderPass, dstOffset, data))
            {
                throw new ArgumentException($"Invalid offset 0x{dstOffset:X} or data size 0x{data.Length:X}.");
            }
        }

        private unsafe bool TryPushData(CommandBufferScoped cbs, Action endRenderPass, int dstOffset, ReadOnlySpan<byte> data)
        {
            if ((dstOffset & 3) != 0 || (data.Length & 3) != 0)
            {
                return false;
            }

            endRenderPass();

            var dstBuffer = GetBuffer(cbs.CommandBuffer, true).Get(cbs, dstOffset, data.Length).Value;

            InsertBufferBarrier(
                _gd,
                cbs.CommandBuffer,
                dstBuffer,
                BufferHolder.DefaultAccessFlags,
                AccessFlags.AccessTransferWriteBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageTransferBit,
                dstOffset,
                data.Length);

            fixed (byte* pData = data)
            {
                for (ulong offset = 0; offset < (ulong)data.Length;)
                {
                    ulong size = Math.Min(MaxUpdateBufferSize, (ulong)data.Length - offset);
                    _gd.Api.CmdUpdateBuffer(cbs.CommandBuffer, dstBuffer, (ulong)dstOffset + offset, size, pData + offset);
                    offset += size;
                }
            }

            InsertBufferBarrier(
                _gd,
                cbs.CommandBuffer,
                dstBuffer,
                AccessFlags.AccessTransferWriteBit,
                BufferHolder.DefaultAccessFlags,
                PipelineStageFlags.PipelineStageTransferBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                dstOffset,
                data.Length);

            // Not flushing commands here causes glitches on Intel (driver bug?)
            if (_gd.Vendor == Vendor.Intel)
            {
                _gd.FlushAllCommands();
            }

            return true;
        }

        public static unsafe void Copy(
            VulkanGraphicsDevice gd,
            CommandBufferScoped cbs,
            Auto<DisposableBuffer> src,
            Auto<DisposableBuffer> dst,
            int srcOffset,
            int dstOffset,
            int size)
        {
            var srcBuffer = src.Get(cbs, srcOffset, size).Value;
            var dstBuffer = dst.Get(cbs, dstOffset, size).Value;

            InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                BufferHolder.DefaultAccessFlags,
                AccessFlags.AccessTransferWriteBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                PipelineStageFlags.PipelineStageTransferBit,
                dstOffset,
                size);

            var region = new BufferCopy((ulong)srcOffset, (ulong)dstOffset, (ulong)size);

            gd.Api.CmdCopyBuffer(cbs.CommandBuffer, srcBuffer, dstBuffer, 1, &region);

            InsertBufferBarrier(
                gd,
                cbs.CommandBuffer,
                dstBuffer,
                AccessFlags.AccessTransferWriteBit,
                BufferHolder.DefaultAccessFlags,
                PipelineStageFlags.PipelineStageTransferBit,
                PipelineStageFlags.PipelineStageAllCommandsBit,
                dstOffset,
                size);
        }

        public static unsafe void InsertBufferBarrier(
            VulkanGraphicsDevice gd,
            CommandBuffer commandBuffer,
            VkBuffer buffer,
            AccessFlags srcAccessMask,
            AccessFlags dstAccessMask,
            PipelineStageFlags srcStageMask,
            PipelineStageFlags dstStageMask,
            int offset,
            int size)
        {
            BufferMemoryBarrier memoryBarrier = new BufferMemoryBarrier()
            {
                SType = StructureType.BufferMemoryBarrier,
                SrcAccessMask = srcAccessMask,
                DstAccessMask = dstAccessMask,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Buffer = buffer,
                Offset = (ulong)offset,
                Size = (ulong)size
            };

            gd.Api.CmdPipelineBarrier(
                commandBuffer,
                srcStageMask,
                dstStageMask,
                0,
                0,
                null,
                1,
                memoryBarrier,
                0,
                null);
        }

        public void WaitForFences()
        {
            _waitable.WaitForFences(_gd.Api, _device);
        }

        public void WaitForFences(int offset, int size)
        {
            _waitable.WaitForFences(_gd.Api, _device, offset, size);
        }

        public bool MayWait(int offset, int size)
        {
            return _waitable.MayWait(_gd.Api, _device, offset, size);
        }

        public Auto<DisposableBuffer> GetBufferI8ToI16(CommandBufferScoped cbs, int offset, int size)
        {
            if (!_cachedConvertedIndexBuffers.TryGetValue(offset, size, out var holder))
            {
                holder = _gd.BufferManager.Create(_gd, (size * 2 + 3) & ~3);

                _gd.HelperShader.ConvertI8ToI16(_gd, cbs, this, holder, offset, size);

                _cachedConvertedIndexBuffers.Add(offset, size, holder);
            }

            return holder.GetBuffer();
        }

        public void Dispose()
        {
            _buffer.Dispose();
            _allocationAuto.Dispose();
            _cachedConvertedIndexBuffers.Dispose();
        }
    }
}
