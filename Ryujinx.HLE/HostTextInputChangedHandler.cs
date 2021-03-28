using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;

namespace Ryujinx.HLE
{
    public delegate void HostTextInputChangedHandler(string text, int cursorPosition, bool isAccept, bool isCancel);
}