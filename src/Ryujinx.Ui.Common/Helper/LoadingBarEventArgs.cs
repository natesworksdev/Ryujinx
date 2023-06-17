using System;

namespace Ryujinx.Ui.Common.Helper
{
    public class LoadingBarEventArgs : EventArgs
    {
        public int Curr { get; set; }
        public int Max { get; set; }
    }
}