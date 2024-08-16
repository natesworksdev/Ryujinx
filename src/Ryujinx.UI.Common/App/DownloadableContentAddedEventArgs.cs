using Ryujinx.UI.Common.Models;
using System;

namespace Ryujinx.UI.App.Common
{
    public class DownloadableContentAddedEventArgs : EventArgs
    {
        public DownloadableContentModel DownloadableContent { get; set; }
    }
}
