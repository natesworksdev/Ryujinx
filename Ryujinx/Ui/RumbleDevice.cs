using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;
using SDL2;
using System;

namespace Ryujinx.Ui
{
    class RumbleDevice
    {
        private bool _dllLoaded = true;
        private bool _rumbleSupported;
        private IntPtr _joystick;
        private IntPtr _haptic;
        private SDL.SDL_HapticEffect _effect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_LEFTRIGHT,
            leftright = new SDL.SDL_HapticLeftRight
            {
                type = SDL.SDL_HAPTIC_LEFTRIGHT,
                length = 1,
                small_magnitude = 0,
                large_magnitude = 0,
            },
        };
        public HidVibrationValue LastVibrationValue { get; private set; } = new HidVibrationValue();

        public RumbleDevice(int index)
        {
            try
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_HAPTIC | SDL.SDL_INIT_JOYSTICK) != 0)
                {
                    Logger.PrintError(LogClass.ServiceHid, "Failed to initialize SDL, error = " + SDL.SDL_GetError());
                }
                _joystick = SDL.SDL_JoystickOpen(index);
                _haptic = SDL.SDL_HapticOpenFromJoystick(_joystick);
                if (_haptic == IntPtr.Zero)
                {
                    Logger.PrintInfo(LogClass.ServiceHid, "Haptic device is null!");
                    Logger.PrintInfo(LogClass.ServiceHid, "SDL error = " + SDL.SDL_GetError());
                }
                if (_haptic == IntPtr.Zero || SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0)
                {
                    if (SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0) Logger.PrintInfo(LogClass.ServiceHid, "Haptic effect leftright not supported!");
                    _rumbleSupported = false;
                }
                else
                {
                    _rumbleSupported = true;
                }
                Logger.PrintInfo(LogClass.ServiceHid, "Initialized rumble device, actual support = " + _rumbleSupported);
            } catch (DllNotFoundException)
            {
                Logger.PrintInfo(LogClass.ServiceHid, "SDL2 DLL not found, silently stubbing");
                _dllLoaded = false;
            }
        }

        public void RumbleMultiple(ReadOnlySpan<HidVibrationValue> values)
        {
            if (!_rumbleSupported)
            {
                if (!values.IsEmpty)
                {
                    LastVibrationValue = values[^1];
                }
                return;
            }
            for (int i = 0; i < values.Length; i++)
            {
                HidVibrationValue value = values[i];
                _effect.leftright.small_magnitude = (ushort)(value.AmplitudeLow * ushort.MaxValue);
                _effect.leftright.large_magnitude = (ushort)(value.AmplitudeHigh * ushort.MaxValue);
                if (_dllLoaded)
                {
                    int effectIndex = SDL.SDL_HapticNewEffect(_haptic, ref _effect);
                    SDL.SDL_HapticRunEffect(_haptic, effectIndex, 1);
                    SDL.SDL_HapticDestroyEffect(_haptic, effectIndex);
                }
                if (i == values.Length - 1)
                {
                    LastVibrationValue = value;
                }
            }
        }

        public void Rumble(HidVibrationValue value)
        {
            RumbleMultiple(new HidVibrationValue[] { value });
        }

        ~RumbleDevice()
        {
            if (_dllLoaded)
            {
                if (_haptic != IntPtr.Zero)
                {
                    SDL.SDL_HapticClose(_haptic);
                }
                SDL.SDL_JoystickClose(_haptic);
            }
        }
    }
}
