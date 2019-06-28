﻿namespace Ryujinx.HLE.Input
{
    public class NpadController : BaseController
    {
        private (NpadColor Left, NpadColor Right) _npadBodyColors;
        private (NpadColor Left, NpadColor Right) _npadButtonColors;

        private bool _isHalf;

        public NpadController(
            ControllerStatus       controllerType,
            Switch                 device,
            (NpadColor, NpadColor) npadBodyColors,
            (NpadColor, NpadColor) npadButtonColors) : base(controllerType, device)
        {
            _npadBodyColors   = npadBodyColors;
            _npadButtonColors = npadButtonColors;
        }

        public override void Connect(ControllerId controllerId)
        {
            if (HidControllerType != ControllerStatus.NpadLeft && HidControllerType != ControllerStatus.NpadRight)
            {
                _isHalf = false;
            }

            ConnectionState = ControllerConnState.ControllerStateConnected;

            if (controllerId == ControllerId.ControllerHandheld)
                ConnectionState |= ControllerConnState.ControllerStateWired;

            ControllerColorDesc singleColorDesc =
                ControllerColorDesc.ColorDescColorsNonexistent;

            ControllerColorDesc splitColorDesc = 0;

            NpadColor singleBodyColor   = NpadColor.Black;
            NpadColor singleButtonColor = NpadColor.Black;

            Initialize(_isHalf,
                (_npadBodyColors.Left,   _npadBodyColors.Right),
                (_npadButtonColors.Left, _npadButtonColors.Right),
                singleColorDesc,
                splitColorDesc,
                singleBodyColor,
                singleButtonColor );

            base.Connect(controllerId);

            var _currentLayout = ControllerLayouts.HandheldJoined;

            switch (HidControllerType)
            {
                case ControllerStatus.NpadLeft:
                    _currentLayout = ControllerLayouts.Left;
                    break;
                case ControllerStatus.NpadRight:
                    _currentLayout = ControllerLayouts.Right;
                    break;
                case ControllerStatus.NpadPair:
                    _currentLayout = ControllerLayouts.Joined;
                    break;
            }

            SetLayout(_currentLayout);
        }
    }
}
