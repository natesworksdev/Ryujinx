using System;
using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Spl
{
    [Service("csrng")]
    class IRandomInterface : IpcService, IDisposable
    {
        private RNGCryptoServiceProvider _rng;
        private bool _isDisposed;

        private object _lock = new object();

        public IRandomInterface(ServiceCtx context)
        {
            _rng = new RNGCryptoServiceProvider();
        }

        [CommandHipc(0)]
        // GetRandomBytes() -> buffer<unknown, 6>
        public ResultCode GetRandomBytes(ServiceCtx context)
        {
            byte[] randomBytes = new byte[context.Request.ReceiveBuff[0].Size];

            _rng.GetBytes(randomBytes);

            context.Memory.Write(context.Request.ReceiveBuff[0].Position, randomBytes);

            return ResultCode.Success;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_isDisposed)
                {
                    _rng.Dispose();

                    _isDisposed = true;
                }
            }
        }
    }
}