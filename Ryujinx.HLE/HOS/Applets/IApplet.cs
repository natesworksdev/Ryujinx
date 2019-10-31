using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;

namespace Ryujinx.HLE.HOS.Applets
{
    interface IApplet
    {
        event EventHandler AppletStateChanged;

        ResultCode Start();
        ResultCode GetResult();
        ResultCode PushInData(IStorage data);
        ResultCode PopOutData(out IStorage data);
    }
}
