using Ryujinx.Graphics.GAL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Memory
{
    internal enum BufferBackingType
    {
        HostMemory,
        DeviceMemory,
        DeviceMemoryWithFlush
    }

    internal struct BufferBackingState
    {
        private const int DeviceLocalSizeThreshold = 256 * 1024; // 256kb

        private const int SetCountThreshold = 100;
        private const int WriteCountThreshold = 50;
        private const int FlushCountThreshold = 5;

        public readonly bool IsDeviceLocal => _activeType != BufferBackingType.HostMemory;

        private readonly SystemMemoryType _systemMemoryType;
        private BufferBackingType _activeType;
        private BufferBackingType _desiredType;

        private bool _canSwap;

        private int _setCount;
        private int _writeCount;
        private int _flushCount;
        private int _flushTemp;
        private int _lastFlushWrite;

        private readonly int _size;

        public BufferBackingState(GpuContext context, Buffer parent, BufferStage stage, IEnumerable<Buffer> baseBuffers = null)
        {
            _size = (int)parent.Size;
            _systemMemoryType = context.Capabilities.MemoryType;

            // Backend managed is always auto, unified memory is always host.
            _desiredType = BufferBackingType.HostMemory;
            _canSwap = _systemMemoryType != SystemMemoryType.BackendManaged && _systemMemoryType != SystemMemoryType.UnifiedMemory;

            if (_canSwap)
            {
                // Might want to start certain buffers as being device local,
                // and the usage might also _lock_ those buffers into being device local.

                BufferStage storageFlags = stage & BufferStage.StorageMask;

                if (parent.Size > DeviceLocalSizeThreshold)
                {
                    _desiredType = BufferBackingType.DeviceMemory;
                }

                if (storageFlags != 0)
                {
                    // Storage buffer bindings may require special treatment.

                    var rawStage = stage & BufferStage.StageMask;

                    if (rawStage == BufferStage.Fragment)
                    {
                        // Fragment read should start device local. Fragment write should always be device local.

                        _desiredType = BufferBackingType.DeviceMemory;

                        if (storageFlags != BufferStage.StorageRead)
                        {
                            _canSwap = false;
                        }
                    }

                    // TODO: atomic access should likely always be device local
                }


                if (baseBuffers != null)
                {
                    foreach (Buffer buffer in baseBuffers)
                    {
                        CombineState(buffer.BackingState);
                    }
                }
            }
        }

        private void CombineState(BufferBackingState oldState)
        {
            _setCount += oldState._setCount;
            _writeCount += oldState._writeCount;
            _flushCount += oldState._flushCount;
            _flushTemp += oldState._flushTemp;
            _lastFlushWrite = -1;

            _canSwap &= oldState._canSwap;

            _desiredType = (BufferBackingType)Math.Max((int)_desiredType, (int)oldState._desiredType);
        }

        public BufferBackingType Active => _activeType;

        public BufferAccess SwitchAccess(Buffer parent)
        {
            BufferAccess access = parent.SparseCompatible ? BufferAccess.SparseCompatible : BufferAccess.Default;

            bool isBackendManaged = _systemMemoryType == SystemMemoryType.BackendManaged;

            if (!isBackendManaged)
            {
                switch (_desiredType)
                {
                    case BufferBackingType.HostMemory:
                        access |= BufferAccess.HostMemory;
                        break;
                    case BufferBackingType.DeviceMemory:
                        access |= BufferAccess.DeviceMemory;
                        break;
                    case BufferBackingType.DeviceMemoryWithFlush:
                        access |= BufferAccess.DeviceMemoryMapped;
                        break;
                }
            }

            _activeType = _desiredType;

            return access;
        }

        public void RecordSet()
        {
            _setCount++;

            ConsiderUseCounts();
        }

        public void RecordFlush()
        {
            if (_lastFlushWrite != _writeCount)
            {
                // If it's on the same page as the last flush, ignore it.
                _lastFlushWrite = _writeCount;
                _flushCount++;
            }
        }

        public readonly bool ShouldChangeBacking()
        {
            return _desiredType != _activeType;
        }

        public bool ShouldChangeBacking(BufferStage stage)
        {
            if (!_canSwap)
            {
                return false;
            }

            BufferStage storageFlags = stage & BufferStage.StorageMask;

            if (storageFlags != 0)
            {
                if (storageFlags != BufferStage.StorageRead)
                {
                    // Storage write.
                    _writeCount++;

                    var rawStage = stage & BufferStage.StageMask;

                    if (rawStage == BufferStage.Fragment)
                    {
                        // Switch to device memory, never swap back.

                        _desiredType = BufferBackingType.DeviceMemory;
                        _canSwap = false;

                        // TODO: atomic access should likely always be device local
                    }
                }

                ConsiderUseCounts();
            }

            return _desiredType != _activeType;
        }

        private void ConsiderUseCounts()
        {
            if (_canSwap)
            {
                if (_writeCount >= WriteCountThreshold || _setCount >= SetCountThreshold || _flushCount >= FlushCountThreshold)
                {
                    if (_flushCount > 0 || _flushTemp-- > 0)
                    {
                        // Buffers that flush should ideally be mapped in host address space for easy copies.
                        // If the buffer is large it will do better on GPU memory, as there will be more writes than data flushes (typically individual pages).
                        // If it is small, then it's likely most of the buffer will be flushed so we want it on host memory, as access is cached.
                        _desiredType = _size > DeviceLocalSizeThreshold ? BufferBackingType.DeviceMemoryWithFlush : BufferBackingType.HostMemory;

                        // It's harder for a buffer that is flushed to revert to another type of mapping.
                        if (_flushCount > 0)
                        {
                            _flushTemp = 1000;
                        }
                    }
                    else if (_writeCount >= WriteCountThreshold)
                    {
                        // Buffers that are written often should ideally be in the device local heap. (Storage buffers)
                        _desiredType = BufferBackingType.DeviceMemory;
                    }
                    else if (_setCount > SetCountThreshold)
                    {
                        // Buffers that have their data set often should ideally be host mapped. (Constant buffers)
                        _desiredType = BufferBackingType.HostMemory;
                    }

                    _lastFlushWrite = -1;
                    _flushCount = 0;
                    _writeCount = 0;
                    _setCount = 0;
                }
            }
        }
    }
}
