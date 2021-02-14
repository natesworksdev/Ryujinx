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
        private ConcurrentQueue<Queue<HidVibrationValue>> _vibrationQueue;
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
        private int _effectIndex = -1;
        public HidVibrationValue LastVibrationValue { get; private set; } = new HidVibrationValue();

        public RumbleDevice(int index, ConcurrentQueue<Queue<HidVibrationValue>> vibrationQueue)
        {
            try
            {
                if (SDL.SDL_Init(SDL.SDL_INIT_HAPTIC) != 0)
                {
                    Logger.Error?.Print(LogClass.ServiceHid, "Failed to initialize SDL, error = " + SDL.SDL_GetError());
                }
                _haptic = SDL.SDL_HapticOpen(index);
                if (_haptic == IntPtr.Zero)
                {
                    Logger.Info?.Print(LogClass.ServiceHid, "Haptic device is null!");
                    Logger.Info?.Print(LogClass.ServiceHid, "SDL error = " + SDL.SDL_GetError());
                }
                if (_haptic == IntPtr.Zero || SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0)
                {
                    if (SDL.SDL_HapticEffectSupported(_haptic, ref _effect) == 0) Logger.Info?.Print(LogClass.ServiceHid, "Haptic effect leftright not supported!");
                    _rumbleSupported = false;
                }
                else
                {
                    _rumbleSupported = true;
                }
                Logger.Info?.Print(LogClass.ServiceHid, "Initialized rumble device, actual support = " + _rumbleSupported);
            } catch (DllNotFoundException)
            {
                Logger.Info?.Print(LogClass.ServiceHid, "SDL2 DLL not found, silently stubbing");
                _dllLoaded = false;
            }
            _vibrationQueue = vibrationQueue;
            if (_rumbleSupported)
            {
                // Create and upload first effect
                _effectIndex = SDL.SDL_HapticNewEffect(_haptic, ref _effect);
                if (_effectIndex < 0)
                {
                    Logger.Error?.Print(LogClass.ServiceHid, "Failed to upload effect, error = " + SDL.SDL_GetError());
                    _rumbleSupported = false;
                } else
                {
                    // Run the effect, we can dynamically update it
                    // (according to SDL2 docs)
                    if (SDL.SDL_HapticRunEffect(_haptic, _effectIndex, SDL.SDL_HAPTIC_INFINITY) < 0)
                    {
                        Logger.Error?.Print(LogClass.ServiceHid, "Failed to run effect, error = " + SDL.SDL_GetError());
                        _rumbleSupported = false;
                    }
                }
            }
        }

        public void ThreadProc()
        {
            while (true)
            {
                Queue<HidVibrationValue> value;
                while (_vibrationQueue.TryDequeue(out value))
                {
                    foreach (HidVibrationValue value2 in value)
                    {
                        if (!SignificantDifference(LastVibrationValue, value2)) continue;
                        _effect.leftright.small_magnitude = Linear(value2.AmplitudeLow);
                        _effect.leftright.large_magnitude = Linear(value2.AmplitudeHigh);
                        if (SDL.SDL_HapticUpdateEffect(_haptic, _effectIndex, ref _effect) < 0)
                        {
                            Logger.Warning?.Print(LogClass.ServiceHid, "Failed to update effect, error = " + SDL.SDL_GetError());
                        } else
                        {
                            Logger.Debug?.Print(LogClass.ServiceHid, "Updated effect with values lA = " + value2.AmplitudeLow + " hA = " + value2.AmplitudeHigh);
                            LastVibrationValue = value2;
                        }
                    }
                }
                Thread.Yield();
            }
        }

        private static bool SignificantDifference(HidVibrationValue oldValue, HidVibrationValue newValue)
        {
            ushort oLA = Linear(oldValue.AmplitudeLow);
            ushort nLA = Linear(newValue.AmplitudeLow);
            ushort oHA = Linear(oldValue.AmplitudeHigh);
            ushort nHA = Linear(newValue.AmplitudeHigh);
            return (oLA >> 3) != (nLA >> 3) || (oHA >> 3) != (nHA >> 3);
        }

        private static ushort EaseOutQuadratic(float x)
        {
            // 32768(1-(1-x)^2)
            return (ushort)(short.MaxValue * (1 - (1 - x) * (1 - x)));
        }

        private static ushort Linear(float x)
        {
            // 32768x
            return (ushort)(short.MaxValue * x);
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
                if (_effectIndex >= 0)
                {
                    SDL.SDL_HapticDestroyEffect(_haptic, _effectIndex);
                }
                if (_haptic != IntPtr.Zero)
                {
                    SDL.SDL_HapticClose(_haptic);
                }
            }
        }
    }
}
