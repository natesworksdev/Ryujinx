using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using StickInputId = Ryujinx.Input.StickInputId;

namespace Ryujinx.Ava.UI.Models
{
    public struct InputModel
    {
        public InputModelType Type;
        public object Value;

        public InputModel(Key key)
        {
            Type = InputModelType.Key;
            Value = key;
        }

        public InputModel(GamepadInputId key)
        {
            Type = InputModelType.GamepadInputId;
            Value = key;
        }

        public InputModel(StickInputId key)
        {
            Type = InputModelType.StickInputId;
            Value = key;
        }

        public Key AsKey()
        {
            if (Type == InputModelType.Key)
            {
                return (Key)Value;
            }

            return Key.Unbound;
        }

        public GamepadInputId AsGid()
        {
            if (Type == InputModelType.GamepadInputId)
            {
                return (GamepadInputId)Value;
            }

            return GamepadInputId.Unbound;
        }

        public StickInputId AsSid()
        {
            if (Type == InputModelType.StickInputId)
            {
                return (StickInputId)Value;
            }

            return StickInputId.Unbound;
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static implicit operator InputModel(Key key) => new(key);
        public static implicit operator InputModel(GamepadInputId gid) => new(gid);
        public static implicit operator InputModel(StickInputId sid) => new(sid);
    }

    public enum InputModelType
    {
        Key,
        GamepadInputId,
        StickInputId
    }
}