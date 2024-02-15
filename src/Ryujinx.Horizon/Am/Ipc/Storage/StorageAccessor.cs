using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.Sf;
using Ryujinx.Horizon.Sdk.Sf.Hipc;
using System;

namespace Ryujinx.Horizon.Am.Ipc.Storage
{
    partial class StorageAccessor : IStorageAccessor
    {
        private readonly Storage _storage;

        public StorageAccessor(Storage storage)
        {
            _storage = storage;
        }

        [CmifCommand(0)]
        public Result GetSize(out long size)
        {
            size = _storage.Data.Length;

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result Write(long offset, [Buffer(HipcBufferFlags.In | HipcBufferFlags.AutoSelect)] ReadOnlySpan<byte> span)
        {
            if (_storage.IsReadOnly)
            {
                return AmResult.ObjectInvalid;
            }

            if (offset > _storage.Data.Length)
            {
                return AmResult.OutOfBounds;
            }

            // TODO: Write

            return Result.Success;
        }

        [CmifCommand(11)]
        public Result Read(long offset, [Buffer(HipcBufferFlags.Out | HipcBufferFlags.AutoSelect)] Span<byte> span)
        {
            if (offset > _storage.Data.Length)
            {
                return AmResult.OutOfBounds;
            }

            // TODO: Read

            return Result.Success;
        }
    }
}
