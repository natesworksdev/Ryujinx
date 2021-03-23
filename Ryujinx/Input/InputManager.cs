using Ryujinx.Gamepad;
using System;

namespace Ryujinx.Input
{
    public class InputManager : IDisposable
    {
        public IGamepadDriver _keyboardDriver;
        public IGamepadDriver _gamepadDriver;

        public InputManager()
        {
            // TODO
        }

        public NpadManager CreateNpadManager()
        {
            return new NpadManager(_keyboardDriver, _gamepadDriver);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _keyboardDriver?.Dispose();
                _gamepadDriver?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
