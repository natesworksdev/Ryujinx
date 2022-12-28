namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class Event
    {
        // TODO: Actually implement this.

        private bool _autoClear;
        private bool _isSignaled;

        public Event(bool autoClear, bool isSignaled = false)
        {
            _autoClear  = autoClear;
            _isSignaled = isSignaled;
        }

        public void Reset()
        {
            _isSignaled = false;
        }

        public void Signal()
        {
            _isSignaled = true;
        }
    }
}
