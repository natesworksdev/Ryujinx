using Ryujinx.Common.Memory;
using System.Runtime.CompilerServices;

namespace Ryujinx.Input
{
    public struct GamepadStateSnapshot
    {
        // NOTE: Update Array size if JoystickInputId is changed.
        private Array2<Array2<float>> _joysticksState;
        // NOTE: Update Array size if GamepadInputId is changed.
        private Array28<bool> _buttonsState;

        public GamepadStateSnapshot(Array2<Array2<float>> joysticksState, Array28<bool> buttonsState)
        {
            _joysticksState = joysticksState;
            _buttonsState = buttonsState;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsPressed(GamepadInputId inputId) => _buttonsState[(int)inputId];


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetPressed(GamepadInputId inputId, bool value) => _buttonsState[(int)inputId] = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float, float) GetStick(StickInputId inputId)
        {
            var result = _joysticksState[(int)inputId];

            return (result[0], result[1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetStick(StickInputId inputId, float x, float y)
        {
            _joysticksState[(int)inputId][0] = x;
            _joysticksState[(int)inputId][1] = y;
        }
    }
}
