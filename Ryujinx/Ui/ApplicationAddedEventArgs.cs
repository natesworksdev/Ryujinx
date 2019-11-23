using System;

namespace Ryujinx.UI
{
    public class ApplicationAddedEventArgs : EventArgs
    {
        public ApplicationData AppData       { get; set; }
        public int             NumAppsFound  { get; set; }
        public int             NumAppsLoaded { get; set; }
    }
}
