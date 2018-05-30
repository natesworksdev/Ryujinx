using Ryujinx.Core.Logging;
using Ryujinx.Core.OsHle.Handles;
using Ryujinx.Core.OsHle.Ipc;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Core.OsHle.Services.Am
{
    class ILibraryAppletAccessor : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private KEvent StateChangedEvent;

        public ILibraryAppletAccessor()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,   GetAppletStateChangedEvent },
                { 10,  Start                      },
                { 30,  GetResult                  },
                { 100, PushInData                 },
                { 101, PopOutData                 }
            };

            StateChangedEvent = new KEvent();
        }

        private const uint LaunchParamsMagic = 0xc79497ca;

        public long GetAppletStateChangedEvent(ServiceCtx Context)
        {
            StateChangedEvent.WaitEvent.Set();

            int Handle = Context.Process.HandleTable.OpenHandle(StateChangedEvent);

            Context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Handle);

            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long Start(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long GetResult(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long PushInData(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceAm, "Stubbed.");

            return 0;
        }

        public long PopOutData(ServiceCtx Context)
        {
            MakeObject(Context, new IStorage(MakeLaunchParams()));

            return 0;
        }

        private byte[] MakeLaunchParams()
        {
            //Size needs to be at least 0x88 bytes otherwise application errors.
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                MS.SetLength(0x88);

                Writer.Write(LaunchParamsMagic);
                Writer.Write(1);  //IsAccountSelected? Only lower 8 bits actually used.
                Writer.Write(1L); //User Id Low (note: User Id needs to be != 0)
                Writer.Write(0L); //User Id High

                return MS.ToArray();
            }
        }
    }
}