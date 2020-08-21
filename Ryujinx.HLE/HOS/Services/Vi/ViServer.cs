using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Services.OsTypes;

namespace Ryujinx.HLE.HOS.Services.Vi
{
    class ViServer : ServerBase
    {
        private SystemEventType _vsyncEvent;
        private SignalableEvent _vsyncEventSignalable;

        public ViServer(KernelContext context) : base(context, "ViServer")
        {
        }

        protected override void Initialize()
        {
            Os.CreateSystemEvent(out _vsyncEvent, EventClearMode.AutoClear, true);

            _vsyncEventSignalable = KernelStatic.GetSignalableEvent(Os.GetWritableHandleOfSystemEvent(ref _vsyncEvent));
        }

        public void SignalVsync() => _vsyncEventSignalable.Signal();
        public int GetVsyncEventHandle() => Os.GetReadableHandleOfSystemEvent(ref _vsyncEvent);
    }
}
