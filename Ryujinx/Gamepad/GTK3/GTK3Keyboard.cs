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

        public KeyboardStateSnaphot GetKeyboardStateSnapshot()
        {
            return IKeyboard.GetStateSnapshot(this);
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

        public bool IsPressed(Key key)
        {
            return _driver.IsPressed(key);
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
