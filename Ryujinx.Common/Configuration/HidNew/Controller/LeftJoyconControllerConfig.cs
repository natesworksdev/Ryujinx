using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration.HidNew.Controller
{
    public class LeftJoyconControllerConfig<Button, Stick> : JoyconConfigControllerCommon<Button, Stick> where Button : unmanaged where Stick : unmanaged
    {
        [JsonIgnore]
        public Button ButtonMinus { get => SpecialButton0; set => SpecialButton0 = value; }
        [JsonIgnore]
        public Button ButtonL { get => Shoulder; set => Shoulder = value; }
        [JsonIgnore]
        public Button ButtonZl { get => Trigger; set => Trigger = value; }
        [JsonIgnore]
        public Button ButtonSl { get => SingleLeftTrigger; set => SingleLeftTrigger = value; }
        [JsonIgnore]
        public Button ButtonSr { get => SingleRightTrigger; set => SingleRightTrigger = value; }
        [JsonIgnore]
        public Button DpadUp { get => ButtonPad0; set => ButtonPad0 = value; }
        [JsonIgnore]
        public Button DpadDown { get => ButtonPad1; set => ButtonPad1 = value; }
        [JsonIgnore]
        public Button DpadLeft { get => ButtonPad2; set => ButtonPad2 = value; }
        [JsonIgnore]
        public Button DpadRight { get => ButtonPad3; set => ButtonPad3 = value; }
    }
}
