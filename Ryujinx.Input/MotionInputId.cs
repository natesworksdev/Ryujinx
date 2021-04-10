namespace Ryujinx.Input
{
    /// <summary>
    /// Represent a motion sensor on a gamepad.
    /// </summary>
    public enum MotionInputId : byte
    {
        /// <summary>
        /// Invalid.
        /// </summary>
        Invalid,

        /// <summary>
        /// Accelerometer.
        /// </summary>
        Accelerometer,

        /// <summary>
        /// Gyroscope.
        /// </summary>
        Gyroscope
    }
}
