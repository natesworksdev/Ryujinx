namespace Ryujinx.Common.Configuration.Hid
{
    public class NpadController
    {
        /// <summary>
        /// Enables or disables controller support
        /// </summary>
        public bool Enabled { get; private set; }

        /// <summary>
        /// Controller Device Index
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Controller Analog Stick Deadzone
        /// </summary>
        public float Deadzone { get; private set; }

        /// <summary>
        /// Controller Trigger Threshold
        /// </summary>
        public float TriggerThreshold { get; private set; }

        /// <summary>
        /// Left JoyCon Controller Bindings
        /// </summary>
        public NpadControllerLeft LeftJoycon { get; private set; }

        /// <summary>
        /// Right JoyCon Controller Bindings
        /// </summary>
        public NpadControllerRight RightJoycon { get; private set; }
    }
}
