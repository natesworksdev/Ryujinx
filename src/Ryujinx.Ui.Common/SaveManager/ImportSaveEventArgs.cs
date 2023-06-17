using LibHac.Fs;
using System;

namespace Ryujinx.Ui.Common.SaveManager
{
    public class ImportSaveEventArgs : EventArgs
    {
        public SaveDataInfo SaveInfo { get; init; }
    }
}