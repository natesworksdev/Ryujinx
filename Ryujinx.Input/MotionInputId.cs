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
        /// Accelerometer (Unified).
        /// </summary>
        /// <remarks>Values are in m/s^2</remarks>
        Accelerometer,

        /// <summary>
        /// Gyroscope (Unified).
        /// </summary>
        /// <remarks>Values are in degrees</remarks>
        Gyroscope,

        /// <summary>
        /// Accelerometer (Left).
        /// </summary>
        /// <remarks>Values are in m/s^2</remarks>
        AccelerometerLeft,

        /// <summary>
        /// Gyroscope (Left).
        /// </summary>
        /// <remarks>Values are in degrees</remarks>
        GyroscopeLeft,

        /// <summary>
        /// Accelerometer (Right).
        /// </summary>
        /// <remarks>Values are in m/s^2</remarks>
        AccelerometerRight,

        /// <summary>
        /// Gyroscope (Right).
        /// </summary>
        /// <remarks>Values are in degrees</remarks>
        GyroscopeRight
    }
}
