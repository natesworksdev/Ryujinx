namespace Ryujinx.Common.Configuration.HidNew.Controller
{
    public class JoyconConfigControllerCommon<Button, Stick> where Button: unmanaged where Stick: unmanaged
    {
        public Stick Joystick { get; set; }
        public bool InvertStickX { get; set; }
        public bool InvertStickY { get; set; }
        public Button StickButton { get; set; }

        public Button ButtonPad0 { get; set; }
        public Button ButtonPad1 { get; set; }
        public Button ButtonPad2 { get; set; }
        public Button ButtonPad3 { get; set; }

        public Button Shoulder { get; set; }

        public Button Trigger { get; set; }

        // Single joycon triggers
        // TODO better name?
        public Button SingleLeftTrigger { get; set; }
        public Button SingleRightTrigger { get; set; }

        // Minus/Plus
        public Button SpecialButton0 { get; set; }

        // Capture/Home
        public Button SpecialButton1 { get; set; }
    }
}
