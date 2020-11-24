using Ryujinx.Horizon.Sdk.OsTypes.Impl;

namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class WaitableHolderBase
    {
        protected WaitableManagerImpl Manager;

        public bool IsLinkedToManager => Manager != null;

        public virtual TriBool Signaled => TriBool.False;

        public virtual int Handle => 0;

        public void SetManager(WaitableManagerImpl manager)
        {
            Manager = manager;
        }

        public virtual TriBool LinkToObjectList() => TriBool.Undefined;

        public virtual void UnlinkFromObjectList() { }

        public virtual long GetWakeUpTime() => -1L;
    }
}
