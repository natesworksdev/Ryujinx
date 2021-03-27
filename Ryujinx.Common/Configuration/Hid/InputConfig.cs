using Ryujinx.Common.Configuration.Hid;
using System;

namespace Ryujinx.Common.Configuration.Hid
{
    public class InputConfig
    {
        public InputBackendType Backend { get; set; }

        /// <summary>
        /// Controller id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///  Controller's Type
        /// </summary>
        public ControllerType ControllerType { get; set; }

        /// <summary>
        ///  Player's Index for the controller
        /// </summary>
        public PlayerIndex PlayerIndex { get; set; }
    }
}
