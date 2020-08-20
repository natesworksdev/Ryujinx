using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Memory;
using Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletCreator;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy
{
    class ILibraryAppletCreator : IpcService
    {
        public ILibraryAppletCreator() { }

        [Command(0)]
        // CreateLibraryApplet(u32, u32) -> object<nn::am::service::ILibraryAppletAccessor>
        public ResultCode CreateLibraryApplet(ServiceCtx context)
        {
            AppletId appletId          = (AppletId)context.RequestData.ReadInt32();
            int      libraryAppletMode = context.RequestData.ReadInt32();

            MakeObject(context, new ILibraryAppletAccessor(appletId, context.Device.System));

            return ResultCode.Success;
        }

        [Command(10)]
        // CreateStorage(u64) -> object<nn::am::service::IStorage>
        public ResultCode CreateStorage(ServiceCtx context)
        {
            long size = context.RequestData.ReadInt64();

            MakeObject(context, new IStorage(new byte[size]));

            return ResultCode.Success;
        }

        [Command(11)]
        // CreateTransferMemoryStorage(b8, u64, handle<copy>) -> object<nn::am::service::IStorage>
        public ResultCode CreateTransferMemoryStorage(ServiceCtx context)
        {
            bool  unknown = context.RequestData.ReadBoolean();
            ulong size    = context.RequestData.ReadUInt64();
            int   handle  = context.Request.HandleDesc.ToCopy[0];

            Map.LocateMappableSpace(out ulong tmemAddress, size);

            KernelStatic.Syscall.MapTransferMemory(handle, tmemAddress, size, KMemoryPermission.Read);

            var data = new byte[(int)size];

            KernelStatic.AddressSpace.Read(tmemAddress, data);
            KernelStatic.Syscall.UnmapTransferMemory(handle, tmemAddress, size);
            KernelStatic.Syscall.CloseHandle(handle);

            MakeObject(context, new IStorage(data));

            return ResultCode.Success;
        }
    }
}