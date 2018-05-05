using ChocolArm64.Memory;
using Ryujinx.Core.OsHle.Handles;
using Ryujinx.Core.OsHle.Ipc;
using Ryujinx.Core.OsHle.Services.Nv.NvGpuAS;
using Ryujinx.Core.OsHle.Services.Nv.NvHostChannel;
using Ryujinx.Core.OsHle.Services.Nv.NvHostCtrl;
using Ryujinx.Core.OsHle.Services.Nv.NvHostCtrlGpu;
using Ryujinx.Core.OsHle.Services.Nv.NvMap;
using System;
using System.Collections.Generic;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class INvDrvServices : IpcService, IDisposable
    {
        private delegate int ProcessIoctl(ServiceCtx Context, int Cmd);

        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private static Dictionary<string, ProcessIoctl> IoctlProcessors =
                   new Dictionary<string, ProcessIoctl>()
        {
            { "/dev/nvhost-as-gpu",   NvGpuASIoctl      .ProcessIoctl },
            { "/dev/nvhost-ctrl",     NvHostCtrlIoctl   .ProcessIoctl },
            { "/dev/nvhost-ctrl-gpu", NvHostCtrlGpuIoctl.ProcessIoctl },
            { "/dev/nvhost-gpu",      NvHostChannelIoctl.ProcessIoctl },
            { "/dev/nvmap",           NvMapIoctl        .ProcessIoctl }
        };

        public static GlobalStateTable Fds { get; private set; }

        private KEvent Event;

        public INvDrvServices()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, Open         },
                { 1, Ioctl        },
                { 2, Close        },
                { 3, Initialize   },
                { 4, QueryEvent   },
                { 8, SetClientPid }
            };

            Event = new KEvent();
        }

        static INvDrvServices()
        {
            Fds = new GlobalStateTable();
        }

        public long Open(ServiceCtx Context)
        {
            long NamePtr = Context.Request.SendBuff[0].Position;

            string Name = AMemoryHelper.ReadAsciiString(Context.Memory, NamePtr);

            int Fd = Fds.Add(Context.Process, new NvFd(Name));

            Context.ResponseData.Write(Fd);
            Context.ResponseData.Write(0);

            return 0;
        }

        public long Ioctl(ServiceCtx Context)
        {
            int Fd  = Context.RequestData.ReadInt32();
            int Cmd = Context.RequestData.ReadInt32();

            NvFd FdData = Fds.GetData<NvFd>(Context.Process, Fd);

            int Result;

            if (IoctlProcessors.TryGetValue(FdData.Name, out ProcessIoctl Process))
            {
                Result = Process(Context, Cmd);
            }
            else
            {
                throw new NotImplementedException($"{FdData.Name} {Cmd:x4}");
            }

            Context.ResponseData.Write(Result);

            return 0;
        }

        public long Close(ServiceCtx Context)
        {
            int Fd = Context.RequestData.ReadInt32();

            Fds.Delete(Context.Process, Fd);

            Context.ResponseData.Write(0);

            return 0;
        }

        public long Initialize(ServiceCtx Context)
        {
            long TransferMemSize   = Context.RequestData.ReadInt64();
            int  TransferMemHandle = Context.Request.HandleDesc.ToCopy[0];

            NvMapIoctl.InitializeNvMap(Context);

            Context.ResponseData.Write(0);

            return 0;
        }

        public long QueryEvent(ServiceCtx Context)
        {
            int Fd      = Context.RequestData.ReadInt32();
            int EventId = Context.RequestData.ReadInt32();

            //TODO: Use Fd/EventId, different channels have different events.
            int Handle = Context.Process.HandleTable.OpenHandle(Event);

            Context.Response.HandleDesc = IpcHandleDesc.MakeCopy(Handle);

            Context.ResponseData.Write(0);

            return 0;
        }

        public long SetClientPid(ServiceCtx Context)
        {
            long Pid = Context.RequestData.ReadInt64();

            Context.ResponseData.Write(0);

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                Event.Dispose();
            }
        }
    }
}