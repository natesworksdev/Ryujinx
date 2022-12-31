namespace Ryujinx.Horizon.Sdk.OsTypes
{
    struct EventType
    {
        public bool Signaled;
        public bool InitiallySignaled;
        public byte ClearMode;
        public InitializationState State;
        public ulong BroadcastCounter;
    }
}
