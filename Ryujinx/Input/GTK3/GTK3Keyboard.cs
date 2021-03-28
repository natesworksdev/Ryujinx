using OpenTK.Mathematics;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using System;
using System.Collections.Generic;

using ConfigKey = Ryujinx.Common.Configuration.Hid.Key;

namespace Ryujinx.Input.GTK3
{
    public class GTK3Keyboard : IKeyboard
    {
        private class ButtonMappingEntry
        {
            public Key From;
            public GamepadInputId To;
        }

        private object _userMappingLock = new object();

        private readonly GTK3KeyboardDriver _driver;
        private StandardKeyboardInputConfig _configuration;
        private List<ButtonMappingEntry> _buttonsUserMapping;

        public GTK3Keyboard(GTK3KeyboardDriver driver, string id, string name)
        {
            _driver = driver;
            Id = id;
            Name = name;
            _buttonsUserMapping = new List<ButtonMappingEntry>();
        }

        private bool HasConfiguration => _configuration != null;

        public string Id { get; }

        public string Name { get; }

        public bool IsConnected => true;

        public GamepadFeaturesFlag Features => GamepadFeaturesFlag.None;

        public void Dispose()
        {
            // No operations
        }

        public KeyboardStateSnapshot GetKeyboardStateSnapshot()
        {
            return IKeyboard.GetStateSnapshot(this);
        }

        private static float ConvertRawStickValue(short value)
        {
            const float ConvertRate = 1.0f / (short.MaxValue + 0.5f);

            return value * ConvertRate;
        }

        private static (short, short) GetStickValues(ref KeyboardStateSnapshot snapshot, JoyconConfigKeyboardStick<ConfigKey> stickConfig)
        {
            short stickX = 0;
            short stickY = 0;

            if (snapshot.IsPressed((Key)stickConfig.StickUp))
            {
                stickY += 1;
            }

            if (snapshot.IsPressed((Key)stickConfig.StickDown))
            {
                stickY -= 1;
            }

            if (snapshot.IsPressed((Key)stickConfig.StickRight))
            {
                stickX += 1;
            }

            if (snapshot.IsPressed((Key)stickConfig.StickLeft))
            {
                stickX -= 1;
            }

            Vector2 stick = new Vector2(stickX, stickY);

            stick.NormalizeFast();

            return ((short)(stick.X * short.MaxValue), (short)(stick.Y * short.MaxValue));
        }

        public GamepadStateSnapshot GetMappedStateSnapshot()
        {
            KeyboardStateSnapshot rawState = GetKeyboardStateSnapshot();
            GamepadStateSnapshot result = default;

            lock (_userMappingLock)
            {
                if (!HasConfiguration)
                {
                    return result;
                }

                foreach (ButtonMappingEntry entry in _buttonsUserMapping)
                {
                    if (entry.From == Key.Unknown || entry.From == Key.Unbound || entry.To == GamepadInputId.Unbound)
                    {
                        continue;
                    }

                    // Do not touch state of button already pressed
                    if (!result.IsPressed(entry.To))
                    {
                        result.SetPressed(entry.To, rawState.IsPressed(entry.From));
                    }
                }

                (short leftStickX, short leftStickY) = GetStickValues(ref rawState, _configuration.LeftJoyconStick);
                (short rightStickX, short rightStickY) = GetStickValues(ref rawState, _configuration.RightJoyconStick);

                result.SetStick(StickInputId.Left, ConvertRawStickValue(leftStickX), ConvertRawStickValue(leftStickY));
                result.SetStick(StickInputId.Right, ConvertRawStickValue(rightStickX), ConvertRawStickValue(rightStickY));
            }

            return result;
        }

        public GamepadStateSnapshot GetStateSnapshot()
        {
            throw new NotImplementedException();
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
            lock (_userMappingLock)
            {
                _configuration = (StandardKeyboardInputConfig)configuration;

                _buttonsUserMapping.Clear();

                // First update sticks
                // TODO: sticks

                // Then left joycon
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftStick, From = (Key)_configuration.LeftJoyconStick.StickButton });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadUp, From = (Key)_configuration.LeftJoycon.DpadUp });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadDown, From = (Key)_configuration.LeftJoycon.DpadDown });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadLeft, From = (Key)_configuration.LeftJoycon.DpadLeft });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadRight, From = (Key)_configuration.LeftJoycon.DpadRight });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Minus, From = (Key)_configuration.LeftJoycon.ButtonMinus });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftShoulder, From = (Key)_configuration.LeftJoycon.ButtonL });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftTrigger, From = (Key)_configuration.LeftJoycon.ButtonZl });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleRightTrigger0, From = (Key)_configuration.LeftJoycon.ButtonSr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleLeftTrigger0, From = (Key)_configuration.LeftJoycon.ButtonSl });

                // Finally right joycon
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightStick, From = (Key)_configuration.RightJoyconStick.StickButton });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.A, From = (Key)_configuration.RightJoycon.ButtonA });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.B, From = (Key)_configuration.RightJoycon.ButtonB });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.X, From = (Key)_configuration.RightJoycon.ButtonX });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Y, From = (Key)_configuration.RightJoycon.ButtonY });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Plus, From = (Key)_configuration.RightJoycon.ButtonPlus });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightShoulder, From = (Key)_configuration.RightJoycon.ButtonR });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightTrigger, From = (Key)_configuration.RightJoycon.ButtonZr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleRightTrigger1, From = (Key)_configuration.RightJoycon.ButtonSr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleLeftTrigger1, From = (Key)_configuration.RightJoycon.ButtonSl });
            }
        }

        public void SetTriggerThreshold(float triggerThreshold)
        {
            // No operations
        }

        public void Rumble(float lowFrequency, float highFrequency, uint durationMs)
        {
            // No operations
        }
    }
}
