using Ryujinx.Graphics.Device;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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

        public int Read(int offset)
        {
            return _state.Read(offset);
        }

        public void Write(int offset, int value)
        {
            var shadowRamControl = _state.State.SetMmeShadowRamControlMode;
            if (shadowRamControl == SetMmeShadowRamControlMode.MethodPassthrough || offset < 0x200)
            {
                _state.Write(offset, value);
            }
            else if (shadowRamControl == SetMmeShadowRamControlMode.MethodTrack ||
                     shadowRamControl == SetMmeShadowRamControlMode.MethodTrackWithFilter)
            {
                _shadowState.Write(offset, value);
                _state.Write(offset, value);
            }
            else /* if (shadowRamControl == SetMmeShadowRamControlMode.MethodReplay) */
            {
                Debug.Assert(shadowRamControl == SetMmeShadowRamControlMode.MethodReplay);
                _state.Write(offset, _shadowState.Read(offset));
            }
        }
    }
}
