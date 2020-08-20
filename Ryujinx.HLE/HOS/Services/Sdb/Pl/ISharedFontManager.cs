using Ryujinx.HLE.HOS.Ipc;

namespace Ryujinx.HLE.HOS.Services.Sdb.Pl
{
    [Service("pl:u")]
    [Service("pl:s")] // 9.0.0+
    class ISharedFontManager : IpcService
    {
        public ISharedFontManager(ServiceCtx context) { }

        [Command(0)]
        // RequestLoad(u32)
        public ResultCode RequestLoad(ServiceCtx context)
        {
            SharedFontType fontType = (SharedFontType)context.RequestData.ReadInt32();

            // We don't need to do anything here because we do lazy initialization
            // on SharedFontManager (the font is loaded when necessary).
            return ResultCode.Success;
        }

        [Command(1)]
        // GetLoadState(u32) -> u32
        public ResultCode GetLoadState(ServiceCtx context)
        {
            SharedFontType fontType = (SharedFontType)context.RequestData.ReadInt32();

            // 1 (true) indicates that the font is already loaded.
            // All fonts are already loaded.
            context.ResponseData.Write(1);

            return ResultCode.Success;
        }

        [Command(2)]
        // GetFontSize(u32) -> u32
        public ResultCode GetFontSize(ServiceCtx context)
        {
            SharedFontType fontType = (SharedFontType)context.RequestData.ReadInt32();

            context.ResponseData.Write(context.Device.System.ServiceServer.SharedFontManager.GetFontSize(fontType));

            return ResultCode.Success;
        }

        [Command(3)]
        // GetSharedMemoryAddressOffset(u32) -> u32
        public ResultCode GetSharedMemoryAddressOffset(ServiceCtx context)
        {
            SharedFontType fontType = (SharedFontType)context.RequestData.ReadInt32();

            context.ResponseData.Write(context.Device.System.ServiceServer.SharedFontManager.GetSharedMemoryAddressOffset(fontType));

            return ResultCode.Success;
        }

        [Command(4)]
        // GetSharedMemoryNativeHandle() -> handle<copy>
        public ResultCode GetSharedMemoryNativeHandle(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(context.Device.System.ServiceServer.SharedFontManager.GetSharedMemoryHandle());

            return ResultCode.Success;
        }

        [Command(5)]
        // GetSharedFontInOrderOfPriority(bytes<8, 1>) -> (u8, u32, buffer<unknown, 6>, buffer<unknown, 6>, buffer<unknown, 6>)
        public ResultCode GetSharedFontInOrderOfPriority(ServiceCtx context)
        {
            long languageCode = context.RequestData.ReadInt64();
            int  loadedCount  = 0;

            for (SharedFontType type = 0; type < SharedFontType.Count; type++)
            {
                int offset = (int)type * 4;

                if (!AddFontToOrderOfPriorityList(context, type, offset))
                {
                    break;
                }

                loadedCount++;
            }

            context.ResponseData.Write(loadedCount);
            context.ResponseData.Write((int)SharedFontType.Count);

            return ResultCode.Success;
        }

        private bool AddFontToOrderOfPriorityList(ServiceCtx context, SharedFontType fontType, int offset)
        {
            long typesPosition = context.Request.ReceiveBuff[0].Position;
            long typesSize     = context.Request.ReceiveBuff[0].Size;

            long offsetsPosition = context.Request.ReceiveBuff[1].Position;
            long offsetsSize     = context.Request.ReceiveBuff[1].Size;

            long fontSizeBufferPosition = context.Request.ReceiveBuff[2].Position;
            long fontSizeBufferSize     = context.Request.ReceiveBuff[2].Size;

            if ((uint)offset + 4 > (uint)typesSize   ||
                (uint)offset + 4 > (uint)offsetsSize ||
                (uint)offset + 4 > (uint)fontSizeBufferSize)
            {
                return false;
            }

            context.Memory.Write((ulong)(typesPosition + offset), (int)fontType);
            context.Memory.Write((ulong)(offsetsPosition + offset),
                context.Device.System.ServiceServer.SharedFontManager.GetSharedMemoryAddressOffset(fontType));
            context.Memory.Write((ulong)(fontSizeBufferPosition + offset),
                context.Device.System.ServiceServer.SharedFontManager.GetFontSize(fontType));

            return true;
        }
    }
}