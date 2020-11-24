using Ryujinx.Horizon.Sdk.OsTypes.Impl;
using Ryujinx.Horizon.Common;
using System;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    public static partial class Os
    {
        public static void InitializeEvent(out EventType evnt, bool signaled, EventClearMode clearMode)
        {
            evnt = new EventType
            {
                Signaled = signaled,
                InitiallySignaled = signaled,
                ClearMode = (byte)clearMode,
                State = EventType.InitializatonState.Initialized
            };
        }
    }
}
