using Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.SystemAppletProxy;
using System.Collections.Concurrent;

namespace Ryujinx.HLE.HOS.SystemState
{
    class AppletStateMgr
    {
        private readonly Horizon _system;

        private ConcurrentQueue<MessageInfo> _messages;

        public FocusState FocusState { get; private set; }

        public IdDictionary AppletResourceUserIds { get; }

        public AppletStateMgr(Horizon system)
        {
            _system = system;
            _messages = new ConcurrentQueue<MessageInfo>();
            AppletResourceUserIds = new IdDictionary();
        }

        public void SetFocus(bool isFocused)
        {
            FocusState = isFocused
                ? FocusState.InFocus
                : FocusState.OutOfFocus;

            EnqueueMessage(MessageInfo.FocusStateChanged);
        }

        public void EnqueueMessage(MessageInfo message)
        {
            _messages.Enqueue(message);

            _system.ServiceServer.AmServer.SignalMessage();
        }

        public bool TryDequeueMessage(out MessageInfo message)
        {
            if (_messages.Count < 2)
            {
                _system.ServiceServer.AmServer.ClearMessage();
            }

            return _messages.TryDequeue(out message);
        }
    }
}