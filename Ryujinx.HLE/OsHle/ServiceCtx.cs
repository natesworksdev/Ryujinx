using ChocolArm64.Memory;
using Ryujinx.HLE.OsHle.Ipc;
using Ryujinx.HLE.OsHle.Kernel;
using System.IO;

namespace Ryujinx.HLE.OsHle
{
    class ServiceCtx
    {
        public Switch       Device       { get; private set; }
        public Process      Process      { get; private set; }
        public AMemory      Memory       { get; private set; }
        public KSession     Session      { get; private set; }
        public IpcMessage   Request      { get; private set; }
        public IpcMessage   Response     { get; private set; }
        public BinaryReader RequestData  { get; private set; }
        public BinaryWriter ResponseData { get; private set; }

        public ServiceCtx(
            Switch       Device,
            Process      Process,
            AMemory      Memory,
            KSession     Session,
            IpcMessage   Request,
            IpcMessage   Response,
            BinaryReader RequestData,
            BinaryWriter ResponseData)
        {
            this.Device       = Device;
            this.Process      = Process;
            this.Memory       = Memory;
            this.Session      = Session;
            this.Request      = Request;
            this.Response     = Response;
            this.RequestData  = RequestData;
            this.ResponseData = ResponseData;
        }
    }
}