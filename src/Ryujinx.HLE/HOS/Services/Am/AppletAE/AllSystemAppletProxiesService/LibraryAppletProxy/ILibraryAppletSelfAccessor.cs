using Ryujinx.Common;
using Ryujinx.HLE.HOS.Applets;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletProxy
{
    class ILibraryAppletSelfAccessor : IpcService
    {
        private readonly AppletStandalone _appletStandalone = new();

        public ILibraryAppletSelfAccessor(ServiceCtx context)
        {
            var commonArgs = new CommonArguments
            {
                AppletVersion = 1,
                StructureSize = 0x20,
                Version = 1,
                ThemeColor = (uint)context.Device.System.State.ThemeColor,
                PlayStartupSound = true,
                SystemTicks = 0,
            };

            ReadOnlySpan<byte> data = MemoryMarshal.Cast<CommonArguments, byte>(MemoryMarshal.CreateReadOnlySpan(ref commonArgs, 1));

            switch (context.Device.Processes.ActiveApplication.ProgramId)
            {
                case 0x0100000000001000:
                    _appletStandalone = new AppletStandalone
                    {
                        AppletId = AppletId.QLaunch,
                        LibraryAppletMode = LibraryAppletMode.AllForeground,
                    };
                    break;
                case 0x0100000000001002:
                    _appletStandalone = new AppletStandalone
                    {
                        AppletId = AppletId.Cabinet,
                        LibraryAppletMode = LibraryAppletMode.AllForeground,
                    };

                    byte[] cabinetInputData = new byte[0x1A8];

                    _appletStandalone.InputData.Enqueue(data.ToArray());
                    _appletStandalone.InputData.Enqueue(cabinetInputData);
                    break;
                case 0x0100000000001009:
                    // Create MiiEdit data.
                    _appletStandalone = new AppletStandalone
                    {
                        AppletId = AppletId.MiiEdit,
                        LibraryAppletMode = LibraryAppletMode.AllForeground,
                    };

                    byte[] miiEditInputData = new byte[0x100];
                    miiEditInputData[0] = 0x03; // Hardcoded unknown value.

                    _appletStandalone.InputData.Enqueue(miiEditInputData);
                    break;
                case 0x010000000000100D:
                    _appletStandalone = new AppletStandalone
                    {
                        AppletId = AppletId.PhotoViewer,
                        LibraryAppletMode = LibraryAppletMode.AllForeground,
                    };

                    byte[] albumArgs = { 2 };

                    _appletStandalone.InputData.Enqueue(data.ToArray());
                    _appletStandalone.InputData.Enqueue(albumArgs);
                    break;
                default:
                    throw new NotImplementedException($"{context.Device.Processes.ActiveApplication.ProgramId} applet is not implemented.");
            }
        }

        [CommandCmif(0)]
        // PopInData() -> object<nn::am::service::IStorage>
        public ResultCode PopInData(ServiceCtx context)
        {
            byte[] appletData = _appletStandalone.InputData.Dequeue();

            if (appletData.Length == 0)
            {
                return ResultCode.NotAvailable;
            }

            MakeObject(context, new IStorage(appletData));

            return ResultCode.Success;
        }

        [CommandCmif(11)]
        // GetLibraryAppletInfo() -> nn::am::service::LibraryAppletInfo
        public ResultCode GetLibraryAppletInfo(ServiceCtx context)
        {
            LibraryAppletInfo libraryAppletInfo = new()
            {
                AppletId = _appletStandalone.AppletId,
                LibraryAppletMode = _appletStandalone.LibraryAppletMode,
            };

            context.ResponseData.WriteStruct(libraryAppletInfo);

            return ResultCode.Success;
        }

        [CommandCmif(14)]
        // GetCallerAppletIdentityInfo() -> nn::am::service::AppletIdentityInfo
        public ResultCode GetCallerAppletIdentityInfo(ServiceCtx context)
        {
            AppletIdentifyInfo appletIdentifyInfo = new()
            {
                AppletId = AppletId.QLaunch,
                TitleId = 0x0100000000001000,
            };

            context.ResponseData.WriteStruct(appletIdentifyInfo);

            return ResultCode.Success;
        }
    }
}
