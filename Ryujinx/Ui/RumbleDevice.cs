using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;
using SDL2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Ui
{
    class RumbleDevice: IDisposable
    {
        private readonly bool _dllLoaded = true;
        private Thread _thread;
        private readonly bool _rumbleSupported;
        private readonly IntPtr _haptic;
        private readonly ConcurrentQueue<Queue<HidVibrationValue>> _vibrationQueue;
        private SDL.SDL_HapticEffect _effect = new SDL.SDL_HapticEffect
        {
            type = SDL.SDL_HAPTIC_LEFTRIGHT,
            leftright = new SDL.SDL_HapticLeftRight
            {
                type = SDL.SDL_HAPTIC_LEFTRIGHT,
                length = 2,
                small_magnitude = 0,
                large_magnitude = 0,
            },
        };
        private readonly int _effectIndex = -1;
        private DateTime _lastUpdate = DateTime.Now;
        private CancellationTokenSource _source = new CancellationTokenSource();
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
                    if ((_effectIndex = SDL.SDL_HapticNewEffect(_haptic, ref _effect)) >= 0)
                    {
                        _rumbleSupported = true;
                    } else
                    {
                        Logger.Error?.Print(LogClass.ServiceHid, "Failed to create effect, error = " + SDL.SDL_GetError());
                        _rumbleSupported = false;
                    }
                }
                Logger.Info?.Print(LogClass.ServiceHid, "Initialized rumble device, actual support = " + _rumbleSupported);
            } catch (DllNotFoundException)
            {
                Logger.Info?.Print(LogClass.ServiceHid, "SDL2 DLL not found, silently stubbing");
                _dllLoaded = false;
            }
            _vibrationQueue = vibrationQueue;
        }

        public void ThreadProc()
        {
            CancellationToken _token = _source.Token;
            while (_vibrationQueue.IsEmpty)
            {
                // yield until we start getting values
                if (_token.IsCancellationRequested) return;
                Thread.Yield();
            }
            while (true)
            {
                while (_vibrationQueue.TryDequeue(out Queue<HidVibrationValue> value))
                {
                    // Only update every 2ms
                    if ((DateTime.Now - _lastUpdate).Milliseconds < 2) continue;
                    foreach (HidVibrationValue value2 in value)
                    {
                        if (!SignificantDifference(LastVibrationValue, value2)) continue;
                        _effect.leftright.small_magnitude = Curve(value2.AmplitudeLow);
                        _effect.leftright.large_magnitude = Curve(value2.AmplitudeHigh);
                        if (SDL.SDL_HapticUpdateEffect(_haptic, _effectIndex, ref _effect) != 0)
                        {
                            Logger.Warning?.Print(LogClass.ServiceHid, "Failed to update effect, error = " + SDL.SDL_GetError());
                        }
                        else
                        {
                            if (SDL.SDL_HapticRunEffect(_haptic, _effectIndex, 1) < 0)
                            {
                                Logger.Warning?.Print(LogClass.ServiceHid, "Failed to run effect, error = " + SDL.SDL_GetError());
                            }
                            else
                            {
                                Logger.Debug?.Print(LogClass.ServiceHid, "Ran effect with values lA = " + value2.AmplitudeLow + " hA = " + value2.AmplitudeHigh);
                                LastVibrationValue = value2;
                            }
                        }
                        LastVibrationValue = value2;
                    }
                    _lastUpdate = DateTime.Now;
                }
                if (_token.IsCancellationRequested) return;
            }
        }

        private static bool SignificantDifference(HidVibrationValue oldValue, HidVibrationValue newValue)
        {
            ushort oLA = Curve(oldValue.AmplitudeLow);
            ushort nLA = Curve(newValue.AmplitudeLow);
            ushort oHA = Curve(oldValue.AmplitudeHigh);
            ushort nHA = Curve(newValue.AmplitudeHigh);
            return (oLA >> 3) != (nLA >> 3) || (oHA >> 3) != (nHA >> 3);
        }

        private static ushort EaseOutQuadratic(float x)
        {
            // 32768(1-(1-x)^2)
            return (ushort)(ushort.MaxValue * (1 - (1 - x) * (1 - x)));
        }

        private static ushort Linear(float x)
        {
            // 32768x
            return (ushort)(ushort.MaxValue * x);
        }

        private static ushort Curve(float x)
        {
            // cubic-bezier(0, 1, 0.85, 1)
            // = 1.45x^3 - 3.45x^2 + 3x
            float x3 = x * x * x;
            float x2 = x * x;
            float res = (1.45f * x3) - (3.45f * x2) + (3 * x);
            return (ushort)(ushort.MaxValue * res);
        }

        public void Start()
        {
            // unnecessary work if dll is not loaded or rumble is not supported
            if (!_dllLoaded || !_rumbleSupported) return;
            _thread = new Thread(() => ThreadProc());
            _thread.Start();
        }

        public void Dispose()
        {
            _source.Cancel();
            if (_thread != null) _thread.Join(500);
            if (_dllLoaded)
            {
                if (_haptic != IntPtr.Zero)
                {
                    _ = SDL.SDL_HapticStopAll(_haptic); // If it doesn't stop, oh well
                    SDL.SDL_HapticClose(_haptic);
                }
            }
        }
    }
}
