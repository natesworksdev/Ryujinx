using LibHac;
using LibHac.Common;
using LibHac.Sf;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Pools;
using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Threading.Tasks;

namespace Ryujinx.HLE.HOS.Services.Fs.FileSystemProxy
{
    class IStorage : DisposableIpcService
    {
        private SharedRef<LibHac.FsSrv.Sf.IStorage> _baseStorage;

        public IStorage(ref SharedRef<LibHac.FsSrv.Sf.IStorage> baseStorage)
        {
            _baseStorage = SharedRef<LibHac.FsSrv.Sf.IStorage>.CreateMove(ref baseStorage);
        }

        [CommandHipc(0)]
        // Read(u64 offset, u64 length) -> buffer<u8, 0x46, 0> buffer
        public ResultCode Read(ServiceCtx context)
        {
            ulong offset = context.RequestData.ReadUInt64();
            ulong size   = context.RequestData.ReadUInt64();

            if (context.Request.ReceiveBuff.Count > 0)
            {
                IpcBuffDesc buffDesc = context.Request.ReceiveBuff[0];

                // Use smaller length to avoid overflows.
                int actualSize = (int)Math.Min(size, 128 * 1024);
                using (PooledBuffer<byte> scratch = BufferPool<byte>.Rent(actualSize))
                {
                    ulong bytesRead = 0;
                    Result result = Result.Success;
                    while (bytesRead < size)
                    {
                        int thisReadSize = (int)Math.Min((long)actualSize, (long)(size - bytesRead));
                        result = _baseStorage.Get.Read((long)(offset + bytesRead), new OutBuffer(scratch.AsSpan), (long)thisReadSize);
                        context.Memory.Write(buffDesc.Position + bytesRead, scratch.AsSpan.Slice(0, thisReadSize));
                        bytesRead += (ulong)thisReadSize;
                    }
                    
                    return (ResultCode)result.Value;
                }
            }

            return ResultCode.Success;
        }

        [CommandHipc(4)]
        // GetSize() -> u64 size
        public ResultCode GetSize(ServiceCtx context)
        {
            Result result = _baseStorage.Get.GetSize(out long size);

            context.ResponseData.Write(size);

            return (ResultCode)result.Value;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _baseStorage.Destroy();
            }
        }
    }
}