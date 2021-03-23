using System;

namespace Ryujinx.Gamepad
{
    public interface IGamepadDriver : IDisposable
    {
        public string DriverName { get; }

        public ReadOnlySpan<string> GamepadsIds { get; }

        public event Action<string> OnGamepadConnected;
        public event Action<string> OnGamepadDisconnected;

        public IGamepad GetGamepad(string id);
    }
}
