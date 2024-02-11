using System;

namespace Ryujinx.Ui.Common.Helper
{
    public record class LoadingBarData
    {
        public int Max { get; set; } = 0;
        public int Curr { get; set; } = 0;
        public bool IsVisible => Max > 0 && Curr < Max;

        public void Reset()
        {
            Max = 0;
            Curr = 0;
        }
    }
}
