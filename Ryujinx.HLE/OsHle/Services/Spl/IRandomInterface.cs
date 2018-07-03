using Ryujinx.HLE.OsHle.Ipc;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ryujinx.HLE.OsHle.Services.Spl
{
    class IRandomInterface : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private RNGCryptoServiceProvider CspRnd;

        public IRandomInterface()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetRandomBytes }
            };

            CspRnd = new RNGCryptoServiceProvider();
        }

        public long GetRandomBytes(ServiceCtx Context)
        {
            (long Position, long Size) = Context.Request.GetBufferType0x21();

            byte[] BytesBuffer = new byte[Size];

            CspRnd.GetBytes(BytesBuffer);

            Context.Memory.WriteBytes(Position, BytesBuffer);

            return 0;
        }
    }
}