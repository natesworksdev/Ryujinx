using Ryujinx.Common.Logging;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using Ryujinx.Horizon.Sdk.Am.Controllers;
using Ryujinx.Horizon.Sdk.Am.Storage;
using Ryujinx.Horizon.Sdk.OsTypes;
using Ryujinx.Horizon.Sdk.Sf;
using System;

namespace Ryujinx.Horizon.Am.Ipc.Controllers
{
    partial class LibraryAppletAccessor : ILibraryAppletAccessor
    {
        private readonly AppletSession _normalSession;
        private readonly AppletSession _interactiveSession;

        private SystemEventType _stateChangedEvent;
        private SystemEventType _normalOutDataEvent;
        private SystemEventType _interactiveOutDataEvent;

        public LibraryAppletAccessor(AppletId appletId)
        {
            Os.CreateSystemEvent(out _stateChangedEvent, EventClearMode.ManualClear, interProcess: true).AbortOnFailure();
            Os.CreateSystemEvent(out _normalOutDataEvent, EventClearMode.ManualClear, interProcess: true).AbortOnFailure();
            Os.CreateSystemEvent(out _interactiveOutDataEvent, EventClearMode.ManualClear, interProcess: true).AbortOnFailure();

            _normalSession = new AppletSession();
            _interactiveSession = new AppletSession();

            _normalSession.DataAvailable += OnNormalOutData;
            _interactiveSession.DataAvailable += OnInteractiveOutData;
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

        [CmifCommand(0)]
        public Result GetAppletStateChangedEvent([CopyHandle] out int arg0)
        {
            arg0 = Os.GetReadableHandleOfSystemEvent(ref _stateChangedEvent);

            return Result.Success;
        }

        [CmifCommand(1)]
        public Result IsCompleted(out bool arg0)
        {
            arg0 = false;
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(10)]
        public Result Start()
        {
            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(20)]
        public Result RequestExit()
        {
            Os.SignalSystemEvent(ref _stateChangedEvent);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(25)]
        public Result Terminate()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(30)]
        public Result GetResult()
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(50)]
        public Result SetOutOfFocusApplicationSuspendingEnabled(bool arg0)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(60)]
        public Result PresetLibraryAppletGpuTimeSliceZero()
        {
            // NOTE: This call reset two internal fields to 0 and one internal field to "true".
            //       It seems to be used only with software keyboard inline.

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(100)]
        public Result PushInData(IStorage data)
        {
            var storage = new Storage.Storage(data);
            _normalSession.Push(storage.Data);

            return Result.Success;
        }

        [CmifCommand(101)]
        public Result PopOutData(out IStorage data)
        {
            if (_interactiveSession.TryPop(out byte[] bytes))
            {
                data = new Storage.Storage(bytes);

                return Result.Success;
            }

            data = new Storage.Storage([]);
            return AmResult.NotAvailable;
        }

        [CmifCommand(102)]
        public Result PushExtraStorage(IStorage data)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(103)]
        public Result PushInteractiveInData(IStorage data)
        {
            var storage = new Storage.Storage(data);

            _interactiveSession.Push(storage.Data);

            return Result.Success;
        }

        [CmifCommand(104)]
        public Result PopInteractiveOutData(out IStorage arg0)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(105)]
        public Result GetPopOutDataEvent(out int handle)
        {
            handle = Os.GetReadableHandleOfSystemEvent(ref _normalOutDataEvent);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(106)]
        public Result GetPopInteractiveOutDataEvent(out int handle)
        {
            handle = Os.GetReadableHandleOfSystemEvent(ref _interactiveOutDataEvent);

            Logger.Stub?.PrintStub(LogClass.ServiceAm);

            return Result.Success;
        }

        [CmifCommand(110)]
        public Result NeedsToExitProcess(out bool arg0)
        {
            arg0 = false;

            return AmResult.Stubbed;
        }

        [CmifCommand(120)]
        public Result GetLibraryAppletInfo(out LibraryAppletInfo arg0)
        {
            throw new System.NotImplementedException();
        }

        [CmifCommand(150)]
        public Result RequestForAppletToGetForeground()
        {
            return AmResult.Stubbed;
        }

        [CmifCommand(160)]
        public Result GetIndirectLayerConsumerHandle(out ulong arg0, ulong arg1, ulong pid)
        {
            throw new System.NotImplementedException();
        }
    }
}
