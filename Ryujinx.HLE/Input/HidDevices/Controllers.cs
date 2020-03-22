using System;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.Common.Logging;
using static Ryujinx.HLE.Input.Hid;


namespace Ryujinx.HLE.Input
{
    public class NpadDevices : BaseDevice
    {
        public struct GamepadInput
        {
            public HidControllerID PlayerId;
            public ControllerKeys Buttons;
            public JoystickPosition LStick;
            public JoystickPosition RStick;
        }

        public struct ControllerConfig
        {
            public HidControllerID PlayerId;
            public ControllerType Type;
        }

        internal HidJoyHoldType JoyHoldType = HidJoyHoldType.Vertical;
        internal bool SixAxisActive = false;  // TODO: link to hidserver when implemented

        private enum FilterState
        {
            Unconfigured = 0,
            Configured = 1,
            Accepted = 2
        }
        private struct NpadConfig
        {
            public ControllerType ConfiguredType;
            public FilterState State;
        }
        private const int _maxControllers = 9;  // Players1-8 and Handheld
        private NpadConfig[] _configuredNpads;
        private ControllerType _supportedStyleSets = ControllerType.ProController | ControllerType.JoyconPair |
                                ControllerType.JoyconLeft | ControllerType.JoyconRight | ControllerType.Handheld;

        public ControllerType SupportedStyleSets
        {
            get { return _supportedStyleSets; }
            set
            {
                if (_supportedStyleSets != value)   // Deal with spamming
                {
                    _supportedStyleSets = value;
                    MatchControllers();
                }

            }
        }

        public HidControllerID PrimaryControllerId { get; set; } = HidControllerID.Unknown;

        private KEvent[] _styleSetUpdateEvents;

        private static readonly Array3<BatteryCharge> _fullBattery;

        public NpadDevices(Switch device, bool active = true) : base(device, active)
        {
            _configuredNpads = new NpadConfig[_maxControllers];

            _styleSetUpdateEvents = new KEvent[_maxControllers];

            for (int i = 0; i < _styleSetUpdateEvents.Length; ++i)
            {
                _styleSetUpdateEvents[i] = new KEvent(_device.System);
            }

            _fullBattery[0] = _fullBattery[1] = _fullBattery[2] = BatteryCharge.Percent100;
        }


        public void AddControllers(params ControllerConfig[] configs)
        {
            for (int i = 0; i < configs.Length; ++i)
            {
                var playerId = configs[i].PlayerId;
                var type = configs[i].Type;
                if (playerId > HidControllerID.Handheld) throw new ArgumentOutOfRangeException("playerId must be Player1-8 or Handheld");

                if (type == ControllerType.Handheld)
                {
                    playerId = HidControllerID.Handheld;
                }

                _configuredNpads[(int)playerId] = new NpadConfig { ConfiguredType = type, State = FilterState.Configured };
            }

            MatchControllers();
        }

        private void MatchControllers()
        {
            PrimaryControllerId = HidControllerID.Unknown;

            for (int i = 0; i < _configuredNpads.Length; ++i)
            {
                ref var p = ref _configuredNpads[i];

                if (p.State == FilterState.Unconfigured) continue;  // Ignore unconfigured

                if ((p.ConfiguredType & _supportedStyleSets) == 0)
                {
                    Logger.PrintWarning(LogClass.Hid, $"ControllerType {p.ConfiguredType} (connected to {(HidControllerID)i}) not supported by game. Removing...");
                    p.State = FilterState.Configured;
                    _device.Hid.SharedMemory.Controllers[i] = new HidController();  // Zero it
                    continue;
                }

                InitController((HidControllerID)i, p.ConfiguredType);
            }

            // Couldn't find any matching configuration. Reassign to something that works.
            if (PrimaryControllerId == HidControllerID.Unknown)
            {
                var npadsTypeList = (ControllerType[])Enum.GetValues(typeof(ControllerType));

                // Skipping None
                for (int i = 1; i < npadsTypeList.Length; ++i)
                {
                    var type = npadsTypeList[i];
                    if ((type & _supportedStyleSets) != 0)
                    {
                        Logger.PrintWarning(LogClass.Hid, $"No matching controllers found. Reassigning input as ControllerType {type}...");
                        
                        InitController(type == ControllerType.Handheld ? HidControllerID.Handheld : HidControllerID.Player1, type);
                        return;
                    }
                }

                Logger.PrintError(LogClass.Hid, "Something went wrong! Couldn't find any appropriate controller!");
            }
        }

        internal ref KEvent GetStyleSetUpdateEvent(HidControllerID playerId)
        {
            return ref _styleSetUpdateEvents[(int)playerId];
        }

        void InitController(HidControllerID playerId, ControllerType type)
        {
            if (type == ControllerType.Handheld) playerId = HidControllerID.Handheld;

            ref var controller = ref _device.Hid.SharedMemory.Controllers[(int)playerId];

            controller = new HidController(); // Zero it

            // TODO: Allow customizing colors at config
            var cHeader = new HidControllerHeader
            {
                IsHalf = false,
                SingleColorBody = NpadColor.Black,
                SingleColorButtons = NpadColor.Black,
                LeftColorBody = NpadColor.BodyNeonBlue,
                LeftColorButtons = NpadColor.Black,
                RightColorBody = NpadColor.BodyNeonRed,
                RightColorButtons = NpadColor.Black
            };

            var commonFlags = DeviceFlags.PowerInfo0Connected | DeviceFlags.PowerInfo1Connected | DeviceFlags.PowerInfo2Connected;

            controller.Misc.BatteryCharge = _fullBattery;

            switch (type)
            {
                case ControllerType.ProController:
                    cHeader.Type = ControllerType.ProController;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.FullKey,
                        DeviceFlags = commonFlags | DeviceFlags.AbxyButtonOriented |
                                        DeviceFlags.PlusButtonCapability | DeviceFlags.MinusButtonCapability

                    };
                    break;
                case ControllerType.Handheld:
                    cHeader.Type = ControllerType.Handheld;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.HandheldLeft | DeviceType.HandheldRight,
                        DeviceFlags = commonFlags | DeviceFlags.AbxyButtonOriented |
                                        DeviceFlags.PlusButtonCapability | DeviceFlags.MinusButtonCapability
                    };
                    break;
                case ControllerType.JoyconPair:
                    cHeader.Type = ControllerType.JoyconPair;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.JoyLeft | DeviceType.JoyRight,
                        DeviceFlags = commonFlags | DeviceFlags.AbxyButtonOriented |
                                        DeviceFlags.PlusButtonCapability | DeviceFlags.MinusButtonCapability
                    };
                    break;
                case ControllerType.JoyconLeft:
                    cHeader.Type = ControllerType.JoyconLeft;
                    cHeader.IsHalf = true;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.JoyLeft,
                        DeviceFlags = commonFlags | DeviceFlags.SlSrButtonOriented |
                                        DeviceFlags.MinusButtonCapability
                    };
                    break;
                case ControllerType.JoyconRight:
                    cHeader.Type = ControllerType.JoyconRight;
                    cHeader.IsHalf = true;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.JoyRight,
                        DeviceFlags = commonFlags | DeviceFlags.SlSrButtonOriented |
                                        DeviceFlags.PlusButtonCapability
                    };
                    break;
                case ControllerType.Pokeball:
                    cHeader.Type = ControllerType.Pokeball;
                    controller.Header = cHeader;
                    controller.Misc = new HidControllerMisc
                    {
                        DeviceType = DeviceType.Palma,
                        DeviceFlags = 0
                    };
                    break;
            }

            if (PrimaryControllerId == HidControllerID.Unknown) PrimaryControllerId = playerId;

            _configuredNpads[(int)playerId].State = FilterState.Accepted;

            _styleSetUpdateEvents[(int)playerId].ReadableEvent.Signal();

            Logger.PrintInfo(LogClass.Hid, $"Connected ControllerType {type} to ControllerID {playerId}");
        }

        static ControllerLayoutType ControllerTypeToLayout(ControllerType cType)
        => cType switch
        {
            ControllerType.ProController => ControllerLayoutType.ProController,
            ControllerType.Handheld => ControllerLayoutType.Handheld,
            ControllerType.JoyconPair => ControllerLayoutType.Dual,
            ControllerType.JoyconLeft => ControllerLayoutType.Left,
            ControllerType.JoyconRight => ControllerLayoutType.Right,
            _ => ControllerLayoutType.Default
        };

        public void SetGamepadsInput(params GamepadInput[] states)
        {
            UpdateAllEntries();

            for (int i = 0; i < states.Length; ++i)
            {
                SetGamepadState(states[i].PlayerId, states[i].Buttons, states[i].LStick, states[i].RStick);
            }
        }

        private void SetGamepadState(HidControllerID playerId, ControllerKeys buttons,
                    JoystickPosition leftJoystick, JoystickPosition rightJoystick)
        {
            if(playerId == HidControllerID.Auto) playerId = PrimaryControllerId;
            if(playerId == HidControllerID.Unknown) return;

            var p = _configuredNpads[(int)playerId];
            if (p.State != FilterState.Accepted) return;

            ref var curNpad = ref _device.Hid.SharedMemory.Controllers[(int)playerId];
            ref var curLayout = ref curNpad.Layouts[(int)ControllerTypeToLayout(curNpad.Header.Type)];

            ref var curEntry = ref curLayout.Entries[(int)curLayout.Header.LatestEntry];

            curEntry.Buttons = buttons;
            curEntry.Joysticks[0] = leftJoystick;
            curEntry.Joysticks[1] = rightJoystick;

            // Mirror data to Default layout just in case
            ref var mainLayout = ref curNpad.Layouts[(int)ControllerLayoutType.Default];
            mainLayout.Entries[(int)mainLayout.Header.LatestEntry] = curEntry;
        }

        public void UpdateAllEntries()
        {
            ref var controllers = ref _device.Hid.SharedMemory.Controllers;
            for (int i = 0; i < controllers.Length; ++i)
            {
                ref var layouts = ref controllers[i].Layouts;
                for (int l = 0; l < layouts.Length; ++l)
                {
                    int curIndex, prevIndex;
                    ref var curLayout = ref layouts[l];
                    curIndex = UpdateEntriesHeader(ref curLayout.Header, out prevIndex);

                    ref var curEntry = ref curLayout.Entries[curIndex];
                    var prevEntry = curLayout.Entries[prevIndex];

                    curEntry.SequenceNumber = prevEntry.SequenceNumber + 1;
                    curEntry.SequenceNumber2 = prevEntry.SequenceNumber2 + 1;

                    if(controllers[i].Header.Type == 0) continue;
                    
                    curEntry.ConnectionState = HidControllerConnectionState.ControllerStateConnected;
                    switch (controllers[i].Header.Type)
                    {
                        case ControllerType.Handheld:
                        case ControllerType.ProController:
                            curEntry.ConnectionState |= HidControllerConnectionState.ControllerStateWired;
                            break;
                        case ControllerType.JoyconPair:
                            curEntry.ConnectionState |= HidControllerConnectionState.JoyLeftConnected | HidControllerConnectionState.JoyRightConnected;
                            break;
                        case ControllerType.JoyconLeft:
                            curEntry.ConnectionState |= HidControllerConnectionState.JoyLeftConnected;
                            break;
                        case ControllerType.JoyconRight:
                            curEntry.ConnectionState |= HidControllerConnectionState.JoyRightConnected;
                            break;
                    }
                }
            }
        }
    }
}