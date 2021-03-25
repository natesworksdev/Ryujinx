using Ryujinx.Configuration.Hid;

namespace Ryujinx.Gamepad
{
    public interface IKeyboard : IGamepad
    {
        // TODO: normal keyboard api and all
        bool IsPressed(Key key);
    }
}
