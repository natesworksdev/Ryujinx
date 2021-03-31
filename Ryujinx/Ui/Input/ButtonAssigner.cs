using System;

namespace Ryujinx.Ui.Input
{
    interface ButtonAssigner : IDisposable
    {
        void Init();

        void ReadInput();

        bool HasAnyButtonPressed();

        bool ShouldCancel();

        string GetPressedButton();
    }
}