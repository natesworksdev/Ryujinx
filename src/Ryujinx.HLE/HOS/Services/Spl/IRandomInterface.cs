using System.Security.Cryptography;

namespace Ryujinx.HLE.HOS.Services.Spl
{
    [Service("csrng")]
    class IRandomInterface : DisposableIpcService
    {
        private readonly RandomNumberGenerator _rng;

#pragma warning disable IDE0052 // Remove unread private member
        private readonly object _lock = new();
#pragma warning restore IDE0052

        public IRandomInterface(ServiceCtx context)
        {
            _rng = RandomNumberGenerator.Create();
        }

        [CommandCmif(0)]
        // GetRandomBytes() -> buffer<unknown, 6>
        public ResultCode GetRandomBytes(ServiceCtx context)
        {
            byte[] randomBytes = new byte[context.Request.ReceiveBuff[0].Size];

            _rng.GetBytes(randomBytes);

            context.Memory.Write(context.Request.ReceiveBuff[0].Position, randomBytes);

            return ResultCode.Success;
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                _rng.Dispose();
            }
        }
    }
}