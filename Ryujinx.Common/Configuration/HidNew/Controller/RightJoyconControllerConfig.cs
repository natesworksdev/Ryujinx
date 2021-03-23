using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration.HidNew.Controller
{
    public class RightJoyconControllerConfig<Button, Stick> : JoyconConfigControllerCommon<Button, Stick> where Button : unmanaged where Stick : unmanaged
    {
        [JsonIgnore]
        public Button ButtonPlus { get => SpecialButton0; set => SpecialButton0 = value; }
        [JsonIgnore]
        public Button ButtonR { get => Shoulder; set => Shoulder = value; }
        [JsonIgnore]
        public Button ButtonZr { get => Trigger; set => Trigger = value; }
        [JsonIgnore]
        public Button ButtonSl { get => SingleLeftTrigger; set => SingleLeftTrigger = value; }
        [JsonIgnore]
        public Button ButtonSr { get => SingleRightTrigger; set => SingleRightTrigger = value; }
        [JsonIgnore]
        public Button ButtonX { get => ButtonPad0; set => ButtonPad0 = value; }
        [JsonIgnore]
        public Button ButtonB { get => ButtonPad1; set => ButtonPad1 = value; }
        [JsonIgnore]
        public Button ButtonY { get => ButtonPad2; set => ButtonPad2 = value; }
        [JsonIgnore]
        public Button ButtonA { get => ButtonPad3; set => ButtonPad3 = value; }
    }
}