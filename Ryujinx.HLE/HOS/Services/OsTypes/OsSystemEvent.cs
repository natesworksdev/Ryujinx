using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Services.OsTypes.Impl;
using System;

namespace Ryujinx.HLE.HOS.Services.OsTypes
{
    static partial class Os
    {
        public static KernelResult CreateSystemEvent(out SystemEventType sysEvent, EventClearMode clearMode, bool interProcess)
        {
            sysEvent = new SystemEventType();

            if (interProcess)
            {
                KernelResult result = InterProcessEvent.Create(ref sysEvent.InterProcessEvent, clearMode);

                if (result != KernelResult.Success)
                {
                    return result;
                }

                sysEvent.State = SystemEventType.InitializatonState.InitializedAsInterProcess;
            }
            else
            {
                throw new NotImplementedException();
            }

            return KernelResult.Success;
        }

        public static void DestroySystemEvent(ref SystemEventType sysEvent)
        {
            var oldState = sysEvent.State;
            sysEvent.State = SystemEventType.InitializatonState.NotInitialized;

            switch (oldState)
            {
                case SystemEventType.InitializatonState.InitializedAsInterProcess:
                    InterProcessEvent.Destroy(ref sysEvent.InterProcessEvent);
                    break;
            }
        }

        public static int DetachReadableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.DetachReadableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int DetachWritableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.DetachWritableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int GetReadableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.GetReadableHandle(ref sysEvent.InterProcessEvent);
        }

        public static int GetWritableHandleOfSystemEvent(ref SystemEventType sysEvent)
        {
            return InterProcessEvent.GetWritableHandle(ref sysEvent.InterProcessEvent);
        }

        public static void SignalSystemEvent(ref SystemEventType sysEvent)
        {
            switch (sysEvent.State)
            {
                case SystemEventType.InitializatonState.InitializedAsInterProcess:
                    InterProcessEvent.Signal(ref sysEvent.InterProcessEvent);
                    break;
            }
        }

        public static void ClearSystemEvent(ref SystemEventType sysEvent)
        {
            switch (sysEvent.State)
            {
                case SystemEventType.InitializatonState.InitializedAsInterProcess:
                    InterProcessEvent.Clear(ref sysEvent.InterProcessEvent);
                    break;
            }
        }
    }
}
