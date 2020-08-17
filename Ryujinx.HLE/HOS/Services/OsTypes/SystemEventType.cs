namespace Ryujinx.HLE.HOS.Services.OsTypes
{
    struct SystemEventType
    {
        public enum InitializatonState : byte
        {
            NotInitialized,
            InitializedAsEvent,
            InitializedAsInterProcess
        }

        public InterProcessEventType InterProcessEvent;
        public InitializatonState State;

        public bool NotInitialized => State == InitializatonState.NotInitialized;
    }
}
