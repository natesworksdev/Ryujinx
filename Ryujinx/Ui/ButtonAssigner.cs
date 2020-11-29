using Ryujinx.Common.Configuration.Hid;

namespace Ryujinx.Ui
{
    interface ButtonAssigner
    {
        void Init();

        void ReadInput();

        bool HasAnyButtonPressed();

        bool ShouldCancel();

        string GetPressedButton();
    }
}