using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Spl
{
    internal class IRandomInterface : IpcService, IDisposable
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private RNGCryptoServiceProvider _rng;

        public IRandomInterface()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, GetRandomBytes }
            };

            _rng = new RNGCryptoServiceProvider();
        }

        public long GetRandomBytes(ServiceCtx context)
        {
            byte[] randomBytes = new byte[context.Request.ReceiveBuff[0].Size];

            _rng.GetBytes(randomBytes);

            context.Memory.WriteBytes(context.Request.ReceiveBuff[0].Position, randomBytes);

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) _rng.Dispose();
        }
    }
}