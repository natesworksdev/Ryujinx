namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public struct SystemEventType
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
