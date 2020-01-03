using static Ryujinx.HLE.Input.Hid;

namespace Ryujinx.HLE.Input
{
    public class BaseController
    {
        protected Switch Device { get; }

        protected ControllerStatus ControllerType;

        private ControllerId      _controllerId;
        private ControllerLayouts _currentLayout;

        private HidControllerHeader _header;
        private HidControllerMisc   _misc;

        protected ControllerConnectionState ConnectionState;

        public BaseController(Switch device, ControllerStatus controllerType)
        {
            Device         = device;
            ControllerType = controllerType;
        }

        protected void Initialize(
            bool isHalf,
            (NpadColor Left, NpadColor Right) bodyColors,
            (NpadColor Left, NpadColor Right) buttonColors,
            ControllerColorDescription        singleColorDesc   = 0,
            ControllerColorDescription        splitColorDesc    = 0,
            NpadColor                         singleBodyColor   = 0,
            NpadColor                         singleButtonColor = 0)
        {
            _header = new HidControllerHeader()
            {
                Type                   = ControllerType,
                IsHalf                 = isHalf,
                LeftColorBody          = bodyColors.Left,
                LeftColorButtons       = buttonColors.Left,
                RightColorBody         = bodyColors.Right,
                RightColorButtons      = buttonColors.Right,
                SingleColorBody        = singleBodyColor,
                SingleColorButtons     = singleButtonColor,
                SplitColorsDescriptor  = splitColorDesc,
                SingleColorsDescriptor = singleColorDesc
            };

            _misc = new HidControllerMisc()
            {
                PowerInfo0BatteryState = BatteryState.Percent100,
                PowerInfo1BatteryState = BatteryState.Percent100,
                PowerInfo2BatteryState = BatteryState.Percent100,
                DeviceType             = ControllerDeviceType.NPadLeftController | ControllerDeviceType.NPadRightController,
                DeviceFlags            = DeviceFlags.PowerInfo0Connected |
                                         DeviceFlags.PowerInfo1Connected |
                                         DeviceFlags.PowerInfo2Connected
            };
        }

        public virtual void Connect(ControllerId controllerId)
        {
            _controllerId = controllerId;

            ref HidSharedMemory sharedMemory = ref Device.Hid.SharedMemory;

            ref HidController controller = ref sharedMemory.Controllers[(int)controllerId];

            controller.Header = _header;
            controller.Misc   = _misc;
        }

        public void SetLayout(ControllerLayouts controllerLayout)
        {
            _currentLayout = controllerLayout;
        }

        public void SendInput(ControllerButtons buttons, JoystickPosition leftStick, JoystickPosition rightStick)
        {
            ref HidSharedMemory sharedMemory = ref Device.Hid.SharedMemory;

            ref HidControllerLayout layout = ref sharedMemory.Controllers[(int)_controllerId].Layouts[(int)_currentLayout];

            layout.Header.NumEntries    = 17;
            layout.Header.MaxEntryIndex = 16;

            layout.Header.LatestEntry = (layout.Header.LatestEntry + 1) % 17;

            layout.Header.TimestampTicks = GetTimestamp();

            ref HidControllerInputEntry entry = ref layout.Entries[(int)layout.Header.LatestEntry];

            entry.Timestamp++;
            entry.Timestamp2++;

            entry.Buttons         = buttons;
            entry.ConnectionState = ConnectionState;
            entry.Joysticks[0]    = leftStick;
            entry.Joysticks[1]    = rightStick;
        }
    }
}
