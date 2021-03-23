namespace Ryujinx.Common.Configuration.HidNew.Controller
{
    public class ControllerInputConfigBase : InputConfig
    {
        /// <summary>
        /// Controller Left Analog Stick Deadzone
        /// </summary>
        public float DeadzoneLeft { get; set; }

        /// <summary>
        /// Controller Right Analog Stick Deadzone
        /// </summary>
        public float DeadzoneRight { get; set; }

        /// <summary>
        /// Controller Trigger Threshold
        /// </summary>
        public float TriggerThreshold { get; set; }
    }
}
