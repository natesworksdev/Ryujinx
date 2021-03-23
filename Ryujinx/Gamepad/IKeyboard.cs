using Ryujinx.Configuration.Hid;

namespace Ryujinx.Gamepad
{
    public interface IKeyboard : IGamepad
    {
        void MapButtonToKey(GamepadInputId inputId, Key key);


        void MapSticknToKey(StickInputId inputId, Key up, Key down, Key left, Key right);

    }
}
