using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class StorageAccessor : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private Storage _storage;

        public StorageAccessor(Storage storage)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  GetSize },
                { 10, Write   },
                { 11, Read    }
            };

            this._storage = storage;
        }

        public long GetSize(ServiceCtx context)
        {
            context.ResponseData.Write((long)_storage.Data.Length);

            return 0;
        }

        public long Write(ServiceCtx context)
        {
            //TODO: Error conditions.
            long writePosition = context.RequestData.ReadInt64();

            (long position, long size) = context.Request.GetBufferType0X21();

            if (size > 0)
            {
                long maxSize = _storage.Data.Length - writePosition;

                if (size > maxSize)
                {
                    size = maxSize;
                }

                byte[] data = context.Memory.ReadBytes(position, size);

                Buffer.BlockCopy(data, 0, _storage.Data, (int)writePosition, (int)size);
            }

            return 0;
        }

        public long Read(ServiceCtx context)
        {
            //TODO: Error conditions.
            long readPosition = context.RequestData.ReadInt64();

            (long position, long size) = context.Request.GetBufferType0X22();

            byte[] data;

            if (_storage.Data.Length > size)
            {
                data = new byte[size];

                Buffer.BlockCopy(_storage.Data, 0, data, 0, (int)size);
            }
            else
            {
                data = _storage.Data;
            }

            context.Memory.WriteBytes(position, data);

            return 0;
        }
    }
}