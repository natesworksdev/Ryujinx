using System;

namespace Ryujinx.UI.App.Common
{
    public class DownloadableContentAddedEventArgs : EventArgs
    {
        public ulong TitleId { get; set; }
        public string ContainerFilePath { get; set; }
        public string NcaPath { get; set; }
    }
}
