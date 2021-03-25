using Ryujinx.Common.Configuration.HidNew;
using Ryujinx.Configuration.Hid;
using System;

namespace Ryujinx.Gamepad.GTK3
{
    public class GTK3Keyboard : IKeyboard
    {
        private readonly GTK3KeyboardDriver _driver;

        public GTK3Keyboard(GTK3KeyboardDriver driver, string id, string name)
        {
            _driver = driver;
            Id = id;
            Name = name;
        }

        public string Id { get; }

        public string Name { get; }

        public bool IsConnected => true;

        public void Dispose()
        {
            // No operations
        }

        public GamepadStateSnapshot GetMappedStateSnapshot()
        {
            throw new NotImplementedException();
        }

        public GamepadStateSnapshot GetStateSnapshot()
        {
            return IGamepad.GetStateSnapshot(this);
        }

        public (float, float) GetStick(StickInputId inputId)
        {
            throw new NotImplementedException();
        }

        public bool IsPressed(GamepadInputId inputId)
        {
            throw new NotImplementedException();
        }

        public void MapButtonToKey(GamepadInputId inputId, Key key)
        {
            throw new NotImplementedException();
        }

        public void MapSticknToKey(StickInputId inputId, Key up, Key down, Key left, Key right)
        {
            throw new NotImplementedException();
        }

        public void SetConfiguration(InputConfig configuration)
        {
            throw new NotImplementedException();
        }

        public void SetTriggerThreshold(float triggerThreshold)
        {
            // No operations
        }
    }
}
