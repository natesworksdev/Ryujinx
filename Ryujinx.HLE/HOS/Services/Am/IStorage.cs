using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    internal class IStorage : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public byte[] Data { get; private set; }

        public IStorage(byte[] data)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, Open }
            };

            Data = data;
        }

        public long Open(ServiceCtx context)
        {
            MakeObject(context, new IStorageAccessor(this));

            return 0;
        }
    }
}