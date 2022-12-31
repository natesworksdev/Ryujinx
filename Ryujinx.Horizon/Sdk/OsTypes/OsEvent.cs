namespace Ryujinx.Horizon.Sdk.OsTypes
{
    static partial class Os
    {
        public static void InitializeEvent(out EventType evnt, bool signaled, EventClearMode clearMode)
        {
            evnt = new EventType
            {
                Signaled = signaled,
                InitiallySignaled = signaled,
                ClearMode = (byte)clearMode,
                State = InitializationState.Initialized
            };
        }
    }
}
