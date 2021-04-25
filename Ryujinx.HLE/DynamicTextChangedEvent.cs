using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Services.Am.AppletOE.ApplicationProxyService.ApplicationProxy.Types;

namespace Ryujinx.HLE
{
    public delegate void DynamicTextChangedEvent(string text, int cursorBegin, int cursorEnd, bool isAccept, bool isCancel, bool force);
}