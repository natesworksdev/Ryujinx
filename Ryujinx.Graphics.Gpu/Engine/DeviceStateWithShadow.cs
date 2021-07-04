using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Engine
{
    interface IShadowState
    {
        SetMmeShadowRamControlMode SetMmeShadowRamControlMode { get; }
    }

    class DeviceStateWithShadow<TState> : IDeviceState where TState : unmanaged, IShadowState
    {
        private readonly DeviceState<TState> _state;
        private readonly DeviceState<TState> _shadowState;

        public ref TState State => ref _state.State;

        public DeviceStateWithShadow(IReadOnlyDictionary<string, RwCallback> callbacks = null, Action<string> debugLogCallback = null)
        {
            _state = new DeviceState<TState>(callbacks, debugLogCallback);
            _shadowState = new DeviceState<TState>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Read(int offset)
        {
            return _state.Read(offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int offset, int value)
        {
            WriteWithRedundancyCheck(offset, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WriteWithRedundancyCheck(int offset, int value)
        {
            var shadowRamControl = _state.State.SetMmeShadowRamControlMode;
            if (shadowRamControl == SetMmeShadowRamControlMode.MethodPassthrough || offset < 0x200)
            {
                return _state.WriteWithRedundancyCheck(offset, value);
            }
            else if (shadowRamControl == SetMmeShadowRamControlMode.MethodTrack ||
                     shadowRamControl == SetMmeShadowRamControlMode.MethodTrackWithFilter)
            {
                _shadowState.Write(offset, value);
                return _state.WriteWithRedundancyCheck(offset, value);
            }
            else /* if (shadowRamControl == SetMmeShadowRamControlMode.MethodReplay) */
            {
                Debug.Assert(shadowRamControl == SetMmeShadowRamControlMode.MethodReplay);
                return _state.WriteWithRedundancyCheck(offset, _shadowState.Read(offset));
            }
        }
    }
}
