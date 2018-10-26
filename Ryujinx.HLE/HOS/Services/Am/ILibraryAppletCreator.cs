using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    internal class ILibraryAppletCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ILibraryAppletCreator()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  CreateLibraryApplet },
                { 10, CreateStorage       }
            };
        }

        public long CreateLibraryApplet(ServiceCtx context)
        {
            MakeObject(context, new ILibraryAppletAccessor(context.Device.System));

            return 0;
        }

        public long CreateStorage(ServiceCtx context)
        {
            long size = context.RequestData.ReadInt64();

            MakeObject(context, new IStorage(new byte[size]));

            return 0;
        }
    }
}