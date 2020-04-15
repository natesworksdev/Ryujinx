namespace Ryujinx.HLE.HOS.Services.Mm.Types
{
    class MultiMediaSession
    {
        public MultiMediaOperationType Type { get; private set; }

        public bool IsAutoClearEvent { get; private set; }
        public uint Id               { get; private set; }
        public uint CurrentValue     { get; private set; }

        public MultiMediaSession(uint id, MultiMediaOperationType type, bool isAutoClearEvent)
        {
            Type             = type;
            Id               = id;
            IsAutoClearEvent = isAutoClearEvent;
            CurrentValue     = 0;
        }

        public void SetAndWait(uint value, int timeout)
        {
            CurrentValue = value;
        }
    }
}
