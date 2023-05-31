using System;

namespace Ryujinx.Ui.App.Common
{
    public sealed class ApplicationAddedEventArgs : EventArgs
    {
        public ApplicationData AppData { get; set; }
    }
}