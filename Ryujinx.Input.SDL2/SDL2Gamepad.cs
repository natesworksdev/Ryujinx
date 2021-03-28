using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using static SDL2.SDL;

namespace Ryujinx.Input.SDL2
{
    class SDL2Gamepad : IGamepad
    {
        private bool HasConfiguration => _configuration != null;

        private class ButtonMappingEntry
        {
            public GamepadInputId From;
            public GamepadInputId To;
        }

        private StandardControllerInputConfig _configuration;

        private static readonly SDL_GameControllerButton[] _buttonsDriverMapping = new SDL_GameControllerButton[(int)GamepadInputId.Count]
        {
            // unbound, ignored.
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,

            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER,

            // NOTE: the left and right trigger are axis, we handle those differently
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,

            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD,            

            // Virtual buttons are invalid, ignored.
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
            SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID,
        };

        private object _userMappingLock = new object();

        private List<ButtonMappingEntry> _buttonsUserMapping;

        private StickInputId[] _stickUserMapping = new StickInputId[(int)StickInputId.Count]
        {
            StickInputId.Left,
            StickInputId.Right
        };

        public GamepadFeaturesFlag Features { get; }

        private IntPtr _gamepadHandle;

        private float _triggerThreshold;

        public SDL2Gamepad(IntPtr gamepadHandle, string driverId)
        {
            _gamepadHandle = gamepadHandle;
            _buttonsUserMapping = new List<ButtonMappingEntry>();

            Name = SDL_GameControllerName(_gamepadHandle);
            Id = driverId;
            Features = GetFeaturesFlag();
            _triggerThreshold = 0.0f;
        }

        private GamepadFeaturesFlag GetFeaturesFlag()
        {
            GamepadFeaturesFlag result = GamepadFeaturesFlag.None;

            if (SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_ACCEL) == SDL_bool.SDL_TRUE &&
                SDL_GameControllerHasSensor(_gamepadHandle, SDL_SensorType.SDL_SENSOR_GYRO) == SDL_bool.SDL_TRUE)
            {
                result |= GamepadFeaturesFlag.Motion;
            }

            int error = SDL_GameControllerRumble(_gamepadHandle, 0, 0, 100);

            if (error == 0)
            {
                result |= GamepadFeaturesFlag.Rumble;
            }

            Logger.Error?.Print(LogClass.Application, result.ToString());

            return result;
        }

        public string Id { get; }
        public string Name { get; }

        public bool IsConnected => SDL_GameControllerGetAttached(_gamepadHandle) == SDL_bool.SDL_TRUE;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SDL_GameControllerClose(_gamepadHandle);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void SetTriggerThreshold(float triggerThreshold)
        {
            _triggerThreshold = triggerThreshold;
        }

        public void Rumble(float lowFrequency, float highFrequency, uint durationMs)
        {
            if (Features.HasFlag(GamepadFeaturesFlag.Rumble))
            {
                ushort lowFrequencyRaw = (ushort)(lowFrequency * ushort.MaxValue);
                ushort highFrequencyRaw = (ushort)(highFrequency * ushort.MaxValue);

                SDL_GameControllerRumble(_gamepadHandle, lowFrequencyRaw, highFrequencyRaw, durationMs);
            }
        }

        public void SetConfiguration(InputConfig configuration)
        {
            lock (_userMappingLock)
            {
                _configuration = (StandardControllerInputConfig)configuration;

                _buttonsUserMapping.Clear();

                // First update sticks
                _stickUserMapping[(int)StickInputId.Left] = (StickInputId)_configuration.LeftJoyconStick.Joystick;
                _stickUserMapping[(int)StickInputId.Right] = (StickInputId)_configuration.RightJoyconStick.Joystick;

                // Then left joycon
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftStick, From = (GamepadInputId)_configuration.LeftJoyconStick.StickButton });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadUp, From = (GamepadInputId)_configuration.LeftJoycon.DpadUp });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadDown, From = (GamepadInputId)_configuration.LeftJoycon.DpadDown });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadLeft, From = (GamepadInputId)_configuration.LeftJoycon.DpadLeft });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.DpadRight, From = (GamepadInputId)_configuration.LeftJoycon.DpadRight });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Minus, From = (GamepadInputId)_configuration.LeftJoycon.ButtonMinus });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftShoulder, From = (GamepadInputId)_configuration.LeftJoycon.ButtonL });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.LeftTrigger, From = (GamepadInputId)_configuration.LeftJoycon.ButtonZl });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleRightTrigger0, From = (GamepadInputId)_configuration.LeftJoycon.ButtonSr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleLeftTrigger0, From = (GamepadInputId)_configuration.LeftJoycon.ButtonSl });

                // Finally right joycon
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightStick, From = (GamepadInputId)_configuration.RightJoyconStick.StickButton });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.A, From = (GamepadInputId)_configuration.RightJoycon.ButtonA });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.B, From = (GamepadInputId)_configuration.RightJoycon.ButtonB });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.X, From = (GamepadInputId)_configuration.RightJoycon.ButtonX });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Y, From = (GamepadInputId)_configuration.RightJoycon.ButtonY });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.Plus, From = (GamepadInputId)_configuration.RightJoycon.ButtonPlus });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightShoulder, From = (GamepadInputId)_configuration.RightJoycon.ButtonR });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.RightTrigger, From = (GamepadInputId)_configuration.RightJoycon.ButtonZr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleRightTrigger1, From = (GamepadInputId)_configuration.RightJoycon.ButtonSr });
                _buttonsUserMapping.Add(new ButtonMappingEntry { To = GamepadInputId.SingleLeftTrigger1, From = (GamepadInputId)_configuration.RightJoycon.ButtonSl });

                SetTriggerThreshold(_configuration.TriggerThreshold);
            }
        }

        public GamepadStateSnapshot GetStateSnapshot()
        {
            return IGamepad.GetStateSnapshot(this);
        }

        public GamepadStateSnapshot GetMappedStateSnapshot()
        {
            GamepadStateSnapshot rawState = GetStateSnapshot();
            GamepadStateSnapshot result = default;

            lock (_userMappingLock)
            {
                if (_buttonsUserMapping.Count == 0)
                {
                    return rawState;
                }

                foreach (ButtonMappingEntry entry in _buttonsUserMapping)
                {
                    if (entry.From == GamepadInputId.Unbound || entry.To == GamepadInputId.Unbound)
                    {
                        continue;
                    }

                    // Do not touch state of button already pressed
                    if (!result.IsPressed(entry.To))
                    {
                        result.SetPressed(entry.To, rawState.IsPressed(entry.From));
                    }
                }

                (float leftStickX, float leftStickY) = rawState.GetStick(_stickUserMapping[(int)StickInputId.Left]);
                (float rightStickX, float rightStickY) = rawState.GetStick(_stickUserMapping[(int)StickInputId.Right]);

                result.SetStick(StickInputId.Left, leftStickX, leftStickY);
                result.SetStick(StickInputId.Right, rightStickX, rightStickY);
            }

            return result;
        }

        private static float ConvertRawStickValue(short value)
        {
            const float ConvertRate = 1.0f / (short.MaxValue + 0.5f);

            return value * ConvertRate;
        }

        public (float, float) GetStick(StickInputId inputId)
        {
            short stickX;
            short stickY;

            if (inputId == StickInputId.Left)
            {
                stickX = SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
                stickY = SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);
            }
            else if (inputId == StickInputId.Right)
            {
                stickX = SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX);
                stickY = SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY);
            }
            else
            {
                throw new NotSupportedException($"{inputId}");
            }

            float resultX = ConvertRawStickValue(stickX);
            float resultY = -ConvertRawStickValue(stickY);

            if (HasConfiguration)
            {
                if ((inputId == StickInputId.Left && _configuration.LeftJoyconStick.InvertStickX) || (inputId == StickInputId.Right && _configuration.RightJoyconStick.InvertStickX))
                {
                    resultX = -resultX;
                }

                if ((inputId == StickInputId.Left && _configuration.LeftJoyconStick.InvertStickY) || (inputId == StickInputId.Right && _configuration.RightJoyconStick.InvertStickY))
                {
                    resultY = -resultY;
                }
            }

            return (resultX, resultY);
        }

        public bool IsPressed(GamepadInputId inputId)
        {
            if (inputId == GamepadInputId.LeftTrigger)
            {
                return ConvertRawStickValue(SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT)) > _triggerThreshold;
            }
            else if (inputId == GamepadInputId.RightTrigger)
            {
                return ConvertRawStickValue(SDL_GameControllerGetAxis(_gamepadHandle, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT)) > _triggerThreshold;
            }
            else if (_buttonsDriverMapping[(int)inputId] == SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
            {
                return false;
            }
            else
            {
                return SDL_GameControllerGetButton(_gamepadHandle, _buttonsDriverMapping[(int)inputId]) == 1;
            }
        }
    }
}
