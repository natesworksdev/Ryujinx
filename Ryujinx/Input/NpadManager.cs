using Ryujinx.Common.Configuration.HidNew;
using Ryujinx.Common.Configuration.HidNew.Controller;
using Ryujinx.Configuration;
using Ryujinx.Gamepad;
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
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DriverConfigurationUpdate(ref NpadController controller, InputConfig config)
        {
            IGamepadDriver targetDriver = _gamepadDriver;

            // FIXME: not working
            /*if (config.GetType().IsGenericType && config.GetType().GetGenericTypeDefinition() == typeof(ControllerInputConfig<,>))
            {
                targetDriver = _gamepadDriver;
            }
            else if (config is KeyboardConfig keyboardConfig)
            {
                targetDriver = _keyboardDriver;
            }*/

            Debug.Assert(targetDriver != null, "Unknown input configuration!");

            if (controller.GamepadDriver != targetDriver || controller.Id != config.Id)
            {
                controller.UpdateDriverConfiguration(targetDriver, config);
            }
        }

        public void UpdateConfiguration(List<InputConfig> inputConfigs)
        {
            lock (_lock)
            {
                foreach (InputConfig inputConfig in inputConfigs)
                {
                    ref NpadController controller = ref _controllers[(int)inputConfig.PlayerIndex];

                    controller?.Dispose();

                    controller = new NpadController();

                    DriverConfigurationUpdate(ref controller, inputConfig);
                }

                // Enforce an update of the property that will be updated by HLE.
                // TODO: move that
                ConfigurationState.Instance.Hid.InputConfigNew.Value = inputConfigs;
            }
        }

        public void Update(Hid hleHid, TamperMachine tamperMachine, List<InputConfig> inputConfigs)
        {
            lock (_lock)
            {
                List<GamepadInput> hleStates = new List<GamepadInput>();

                foreach (InputConfig inputConfig in inputConfigs)
                {
                    NpadController controller = _controllers[(int)inputConfig.PlayerIndex];

                    DriverConfigurationUpdate(ref controller, inputConfig);

                    controller.Update();

                    GamepadInput state = controller.GetHLEState();

                    state.Buttons |= hleHid.UpdateStickButtons(state.LStick, state.RStick);

                    hleStates.Add(state);
                }

                hleHid.Npads.Update(hleStates);
                tamperMachine.UpdateInput(hleStates);

                // TODO: Six axis
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < _controllers.Length; i++)
                {
                    _controllers[i]?.Dispose();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
