using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using NotImplementedException = System.NotImplementedException;

namespace Ryujinx.Common.Configuration.Hid.Controller.Motion
{
    public class MotionConfigController
    {
        public MotionInputBackendType MotionBackend { get; set; }

        /// <summary>
        /// Gyro Sensitivity
        /// </summary>
        public int Sensitivity { get; set; }

        /// <summary>
        /// Gyro Deadzone
        /// </summary>
        public double GyroDeadzone { get; set;}

        /// <summary>
        /// Enable Motion Controls
        /// </summary>
        public bool EnableMotion { get; set; }
    }
}
