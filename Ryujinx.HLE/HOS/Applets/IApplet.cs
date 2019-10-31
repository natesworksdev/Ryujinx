using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.Collections.Generic;
using System.Text;

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
