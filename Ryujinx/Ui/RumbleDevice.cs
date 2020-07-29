using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;
using SDL2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Ui
{
    class RumbleDevice
    {
        private bool _dllLoaded = true;
        private Thread _thread;
        private bool _rumbleSupported;
        private IntPtr _haptic;
        private ConcurrentQueue<HidVibrationValue> _vibrationQueue;
        private SDL.SDL_HapticEffect _effect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_LEFTRIGHT,
            leftright = new SDL.SDL_HapticLeftRight
            {
                type = SDL.SDL_HAPTIC_LEFTRIGHT,
                length = uint.MaxValue,
                small_magnitude = 0,
                large_magnitude = 0,
            },
        };
        public HidVibrationValue LastVibrationValue { get; private set; } = new HidVibrationValue();

        public RumbleDevice(int index, ConcurrentQueue<HidVibrationValue> vibrationQueue)
        {
            try
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_HAPTIC) != 0)
                {
                    Logger.PrintError(LogClass.ServiceHid, "Failed to initialize SDL, error = " + SDL.SDL_GetError());
                }
                _haptic = SDL.SDL_HapticOpen(index);
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
            _vibrationQueue = vibrationQueue;
        }

        public void ThreadProc()
        {
            while (true)
            {
                HidVibrationValue value;
                while (_vibrationQueue.TryDequeue(out value))
                {
                    _effect.leftright.small_magnitude = (ushort)(value.AmplitudeLow * short.MaxValue);
                    _effect.leftright.large_magnitude = (ushort)(value.AmplitudeHigh * short.MaxValue);
                    int effectIndex = SDL.SDL_HapticNewEffect(_haptic, ref _effect);
                    SDL.SDL_HapticRunEffect(_haptic, effectIndex, 1);
                    SDL.SDL_HapticDestroyEffect(_haptic, effectIndex);
                    LastVibrationValue = value;
                }
                Thread.Yield();
            }
        }

        public void Start()
        {
            // unnecessary work if dll is not loaded or rumble is not supported
            if (!_dllLoaded || !_rumbleSupported) return;
            _thread = new Thread(() => ThreadProc());
            _thread.Start();
        }

        ~RumbleDevice()
        {
            if (_thread != null) _thread.Join(500);
            if (_dllLoaded)
            {
                if (_haptic != IntPtr.Zero)
                {
                    SDL.SDL_HapticClose(_haptic);
                }
            }
        }
    }
}
