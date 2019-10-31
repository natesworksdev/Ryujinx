using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class StubApplet : IApplet
    {
        public event EventHandler AppletStateChanged;

        public ResultCode Start()
        {
            Logger.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        public ResultCode PushInData(IStorage data)
        {
            Logger.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }

        public ResultCode PopOutData(out IStorage data)
        {
            Logger.PrintStub(LogClass.ServiceAm);

            data = null;

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            Logger.PrintStub(LogClass.ServiceAm);

            return ResultCode.Success;
        }
    }
}
