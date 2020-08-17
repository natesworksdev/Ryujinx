using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Services.OsTypes;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletCreator
{
    class ILibraryAppletAccessor : IpcService, IDisposable
    {
        private IApplet _applet;

        private AppletSession _normalSession;
        private AppletSession _interactiveSession;

        private SystemEventType _stateChangedEvent;
        private SystemEventType _normalOutDataEvent;
        private SystemEventType _interactiveOutDataEvent;

        public ILibraryAppletAccessor(AppletId appletId, Horizon system)
        {
            Os.CreateSystemEvent(out _stateChangedEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _normalOutDataEvent, EventClearMode.AutoClear, true);
            Os.CreateSystemEvent(out _interactiveOutDataEvent, EventClearMode.AutoClear, true);

            _applet = AppletManager.Create(appletId, system);

            _normalSession      = new AppletSession();
            _interactiveSession = new AppletSession();

            _applet.AppletStateChanged        += OnAppletStateChanged;
            _normalSession.DataAvailable      += OnNormalOutData;
            _interactiveSession.DataAvailable += OnInteractiveOutData;

            Logger.Info?.Print(LogClass.ServiceAm, $"Applet '{appletId}' created.");
        }

        private void OnAppletStateChanged(object sender, EventArgs e)
        {
            Os.SignalSystemEvent(ref _stateChangedEvent);
        }

        private void OnNormalOutData(object sender, EventArgs e)
        {
            Os.SignalSystemEvent(ref _normalOutDataEvent);
        }

        private void OnInteractiveOutData(object sender, EventArgs e)
        {
            Os.SignalSystemEvent(ref _interactiveOutDataEvent);
        }

        [Command(0)]
        // GetAppletStateChangedEvent() -> handle<copy>
        public ResultCode GetAppletStateChangedEvent(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _stateChangedEvent));

            return ResultCode.Success;
        }

        [Command(10)]
        // Start()
        public ResultCode Start(ServiceCtx context)
        {
            return (ResultCode)_applet.Start(_normalSession.GetConsumer(), _interactiveSession.GetConsumer());
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
            IStorage data = GetObject<IStorage>(context, 0);

            _normalSession.Push(data.Data);

            return ResultCode.Success;
        }

        [Command(101)]
        // PopOutData() -> object<nn::am::service::IStorage>
        public ResultCode PopOutData(ServiceCtx context)
        {
            if (_normalSession.TryPop(out byte[] data))
            {
                MakeObject(context, new IStorage(data));

                Os.ClearSystemEvent(ref _normalOutDataEvent);

                return ResultCode.Success;
            }

            return ResultCode.NotAvailable;
        }

        [Command(103)]
        // PushInteractiveInData(object<nn::am::service::IStorage>)
        public ResultCode PushInteractiveInData(ServiceCtx context)
        {
            IStorage data = GetObject<IStorage>(context, 0);

            _interactiveSession.Push(data.Data);

            return ResultCode.Success;
        }

        [Command(104)]
        // PopInteractiveOutData() -> object<nn::am::service::IStorage>
        public ResultCode PopInteractiveOutData(ServiceCtx context)
        {
            if (_interactiveSession.TryPop(out byte[] data))
            {
                MakeObject(context, new IStorage(data));

                Os.ClearSystemEvent(ref _interactiveOutDataEvent);

                return ResultCode.Success;
            }

            return ResultCode.NotAvailable;
        }

        [Command(105)]
        // GetPopOutDataEvent() -> handle<copy>
        public ResultCode GetPopOutDataEvent(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _normalOutDataEvent));

            return ResultCode.Success;
        }

        [Command(106)]
        // GetPopInteractiveOutDataEvent() -> handle<copy>
        public ResultCode GetPopInteractiveOutDataEvent(ServiceCtx context)
        {
            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Os.GetReadableHandleOfSystemEvent(ref _interactiveOutDataEvent));

            return ResultCode.Success;
        }

        [Command(110)]
        // NeedsToExitProcess()
        public ResultCode NeedsToExitProcess(ServiceCtx context)
        {
            return ResultCode.Stubbed;
        }

        [Command(150)]
        // RequestForAppletToGetForeground()
        public ResultCode RequestForAppletToGetForeground(ServiceCtx context)
        {
            return ResultCode.Stubbed;
        }

        [Command(160)] // 2.0.0+
        // GetIndirectLayerConsumerHandle() -> u64 indirect_layer_consumer_handle
        public ResultCode GetIndirectLayerConsumerHandle(ServiceCtx context)
        {
            /*
            if (indirectLayerConsumer == null)
            {
                return ResultCode.ObjectInvalid;
            }
            */

            // TODO: Official sw uses this during LibraryApplet creation when LibraryAppletMode is 0x3.
            //       Since we don't support IndirectLayer and the handle couldn't be 0, it's fine to return 1.

            ulong indirectLayerConsumerHandle = 1;

            context.ResponseData.Write(indirectLayerConsumerHandle);

            Logger.Stub?.PrintStub(LogClass.ServiceAm, new { indirectLayerConsumerHandle });

            return ResultCode.Success;
        }

        public void Dispose()
        {
            Os.DestroySystemEvent(ref _stateChangedEvent);
            Os.DestroySystemEvent(ref _normalOutDataEvent);
            Os.DestroySystemEvent(ref _interactiveOutDataEvent);
        }
    }
}
