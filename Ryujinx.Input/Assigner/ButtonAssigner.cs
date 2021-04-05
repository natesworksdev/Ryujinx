namespace Ryujinx.Input.Assigner
{
    public interface ButtonAssigner
    {
        void Init();

        void ReadInput();

        bool HasAnyButtonPressed();

        bool ShouldCancel();

        string GetPressedButton();
    }
}