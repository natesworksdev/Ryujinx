using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Am.AppletAE.Storage;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletCreator
{
    class ILibraryAppletAccessor : IpcService
    {
        private IApplet _applet;

        private KEvent _stateChangedEvent;

        public ILibraryAppletAccessor(AppletId appletId, Horizon system)
        {
            _stateChangedEvent = new KEvent(system);
            _applet            = AppletManager.Create(appletId, system);

            _applet.AppletStateChanged += OnAppletStateChanged;
        }

        private void OnAppletStateChanged(object sender, EventArgs e)
        {
            _stateChangedEvent.ReadableEvent.Signal();
        }

        [Command(0)]
        // GetAppletStateChangedEvent() -> handle<copy>
        public ResultCode GetAppletStateChangedEvent(ServiceCtx context)
        {
            _stateChangedEvent.ReadableEvent.Signal();

            if (context.Process.HandleTable.GenerateHandle(_stateChangedEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }

        [Command(10)]
        // Start()
        public ResultCode Start(ServiceCtx context)
        {
            return (ResultCode)_applet.Start();
        }

        [Command(30)]
        // GetResult()
        public ResultCode GetResult(ServiceCtx context)
        {
            return (ResultCode)_applet.GetResult();
        }

        [Command(100)]
        // PushInData(object<nn::am::service::IStorage>)
        public ResultCode PushInData(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        [Command(101)]
        // PopOutData() -> object<nn::am::service::IStorage>
        public ResultCode PopOutData(ServiceCtx context)
        {
            var result = _applet.PopOutData(out IStorage storage);

            if(storage != null)
            {
                MakeObject(context, storage);
            }
            
            return (ResultCode)result;
        }
    }
}