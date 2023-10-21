namespace Ryujinx.HLE.HOS.Services.Caps
{
    [Service("caps:a")]
    class IAlbumAccessorService : IpcService
    {
        public IAlbumAccessorService(ServiceCtx context) { }

        [CommandCmif(5)]
        // IsAlbumMounted() -> bool
        public ResultCode IsAlbumMounted(ServiceCtx context)
        {
            context.ResponseData.Write(true);

            return ResultCode.Success;
        }

        [CommandCmif(18)]
        // GetAppletProgramIdTable()
        public ResultCode GetAppletProgramIdTable(ServiceCtx context)
        {
            context.ResponseData.Write(0x100000000001000);
            context.ResponseData.Write(0x100000000001fff);

            return ResultCode.Success;
        }

        [CommandCmif(101)]
        // GetAlbumFileListEx0()
        public ResultCode GetAlbumFileListEx0(ServiceCtx context)
        {
            return ResultCode.Success;
        }

        [CommandCmif(401)]
        // GetAutoSavingStorage()
        public ResultCode GetAutoSavingStorage(ServiceCtx context)
        {
            context.ResponseData.Write(true);

            return ResultCode.Success;
        }
    }
}
