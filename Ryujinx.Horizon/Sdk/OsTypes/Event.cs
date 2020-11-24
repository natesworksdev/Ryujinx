namespace Ryujinx.Horizon.Sdk.OsTypes
{
    class Event
    {
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
    }
}
