using Ryujinx.UI.Common.Models;
using System;

namespace Ryujinx.UI.App.Common
{
    public class TitleUpdateAddedEventArgs : EventArgs
    {
        public TitleUpdateModel TitleUpdate { get; set; }
    }
}
