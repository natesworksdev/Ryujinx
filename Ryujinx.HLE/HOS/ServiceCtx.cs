using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Memory;
using System.IO;

namespace Ryujinx.HLE.HOS
{
    class ServiceCtx
    {
        public Switch Device { get; }
        public IAddressSpaceManager Memory { get; }
        public IpcMessage Request { get; }
        public IpcMessage Response { get; }
        public BinaryReader RequestData { get; }
        public BinaryWriter ResponseData { get; }

        public ServiceCtx(
            Switch device,
            IAddressSpaceManager memory,
            IpcMessage request,
            IpcMessage response,
            BinaryReader requestData,
            BinaryWriter responseData)
        {
            Device = device;
            Memory = memory;
            Request = request;
            Response = response;
            RequestData = requestData;
            ResponseData = responseData;
        }
    }
}