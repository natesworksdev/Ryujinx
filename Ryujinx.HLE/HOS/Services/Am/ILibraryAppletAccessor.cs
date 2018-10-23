using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class LibraryAppletAccessor : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private KEvent _stateChangedEvent;

        public LibraryAppletAccessor(Horizon system)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,   GetAppletStateChangedEvent },
                { 10,  Start                      },
                { 30,  GetResult                  },
                { 100, PushInData                 },
                { 101, PopOutData                 }
            };

            _stateChangedEvent = new KEvent(system);
        }

        public long GetAppletStateChangedEvent(ServiceCtx context)
        {
            _stateChangedEvent.ReadableEvent.Signal();

            if (context.Process.HandleTable.GenerateHandle(_stateChangedEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long Start(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long GetResult(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long PushInData(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long PopOutData(ServiceCtx context)
        {
            MakeObject(context, new Storage(StorageHelper.MakeLaunchParams()));

            return 0;
        }
    }
}