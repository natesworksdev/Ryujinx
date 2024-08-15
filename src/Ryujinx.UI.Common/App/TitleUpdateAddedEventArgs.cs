using System;

namespace Ryujinx.UI.App.Common
{
    public class TitleUpdateAddedEventArgs : EventArgs
    {
        public ulong TitleId { get; set; }
        public string FilePath { get; set; }
    }
}
