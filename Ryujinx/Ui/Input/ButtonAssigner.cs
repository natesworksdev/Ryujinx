using System;

namespace Ryujinx.Ui.Input
{
    // TODO: make disposable
    interface ButtonAssigner : IDisposable
    {
        void Init();

        void ReadInput();

        bool HasAnyButtonPressed();

        bool ShouldCancel();

        string GetPressedButton();
    }
}