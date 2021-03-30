using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Configuration.Hid.Controller;
using Ryujinx.Common.Configuration.Hid.Keyboard;
using Ryujinx.Configuration;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Hid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    public class NpadManager : IDisposable
    {
        private object _lock = new object();

        private const int MaxControllers = 9;

        private NpadController[] _controllers;

        private readonly IGamepadDriver _keyboardDriver;
        private readonly IGamepadDriver _gamepadDriver;

        public NpadManager(IGamepadDriver keyboardDriver, IGamepadDriver gamepadDriver)
        {
            _controllers = new NpadController[MaxControllers];

            _keyboardDriver = keyboardDriver;
            _gamepadDriver = gamepadDriver;

            _gamepadDriver.OnGamepadConnected += HandleOnGamepadConnected;
            _gamepadDriver.OnGamepadDisconnected += HandleOnGamepadDisconnected;
        }

        private void HandleOnGamepadDisconnected(string obj)
        {
            // Force input reload
            ReloadConfiguration(ConfigurationState.Instance.Hid.InputConfig.Value);
        }

        private void HandleOnGamepadConnected(string id)
        {
            // Force input reload
            ReloadConfiguration(ConfigurationState.Instance.Hid.InputConfig.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DriverConfigurationUpdate(ref NpadController controller, InputConfig config)
        {
            IGamepadDriver targetDriver = _gamepadDriver;

            if (config is StandardControllerInputConfig)
            {
                targetDriver = _gamepadDriver;
            }
            else if (config is StandardKeyboardInputConfig)
            {
                targetDriver = _keyboardDriver;
            }

            Debug.Assert(targetDriver != null, "Unknown input configuration!");

            if (controller.GamepadDriver != targetDriver || controller.Id != config.Id)
            {
                return controller.UpdateDriverConfiguration(targetDriver, config);
            }
            else
            {
                return controller.GamepadDriver != null;
            }
        }

        public void ReloadConfiguration(List<InputConfig> inputConfigs)
        {
            lock (_lock)
            {
                for (int i = 0; i < _controllers.Length; i++)
                {
                    _controllers[i]?.Dispose();
                    _controllers[i] = null;
                }

                foreach (InputConfig inputConfig in inputConfigs)
                {
                    NpadController controller = new NpadController();

                    bool isValid = DriverConfigurationUpdate(ref controller, inputConfig);

                    if (!isValid)
                    {
                        controller.Dispose();
                    }
                    else
                    {
                        _controllers[(int)inputConfig.PlayerIndex] = controller;
                    }
                }

                // Enforce an update of the property that will be updated by HLE.
                // TODO: move that
                ConfigurationState.Instance.Hid.InputConfig.Value = inputConfigs;
            }
        }

        public void Update(Hid hleHid, TamperMachine tamperMachine, List<InputConfig> inputConfigs)
        {
            lock (_lock)
            {
                List<GamepadInput> hleInputStates = new List<GamepadInput>();
                List<SixAxisInput> hleMotionStates = new List<SixAxisInput>(NpadDevices.MaxControllers);

                foreach (InputConfig inputConfig in inputConfigs)
                {
                    GamepadInput inputState = default;
                    SixAxisInput motionState = default;

                    NpadController controller = _controllers[(int)inputConfig.PlayerIndex];

                    // Is a controller connected?
                    if (controller != null)
                    {
                        DriverConfigurationUpdate(ref controller, inputConfig);

                        controller.UpdateUserConfiguration(inputConfig);
                        controller.Update();

                        inputState = controller.GetHLEInputState();

                        inputState.Buttons |= hleHid.UpdateStickButtons(inputState.LStick, inputState.RStick);

                        motionState = controller.GetHLEMotionState();
                    }
                    else
                    {
                        // Ensure that orientation isn't null
                        motionState.Orientation = new float[9];
                    }

                    inputState.PlayerId = (HLE.HOS.Services.Hid.PlayerIndex)inputConfig.PlayerIndex;
                    motionState.PlayerId = (HLE.HOS.Services.Hid.PlayerIndex)inputConfig.PlayerIndex;

                    hleInputStates.Add(inputState);
                    hleMotionStates.Add(motionState);
                }

                hleHid.Npads.Update(hleInputStates);
                hleHid.Npads.UpdateSixAxis(hleMotionStates);
                tamperMachine.UpdateInput(hleInputStates);

                // TODO: Six axis
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    _gamepadDriver.OnGamepadConnected -= HandleOnGamepadConnected;
                    _gamepadDriver.OnGamepadDisconnected -= HandleOnGamepadDisconnected;

                    for (int i = 0; i < _controllers.Length; i++)
                    {
                        _controllers[i]?.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public KeyboardHotkeyState GetHotkeyState()
        {
            KeyboardHotkeyState state = KeyboardHotkeyState.None;

            if (_keyboardDriver != null)
            {
                IKeyboard keyboard = (IKeyboard)_keyboardDriver.GetGamepad("0");

                if (keyboard.IsPressed((Key)ConfigurationState.Instance.Hid.Hotkeys.Value.ToggleVsync))
                {
                    state |= KeyboardHotkeyState.ToggleVSync;
                }
            }


            return state;
        }
    }
}
