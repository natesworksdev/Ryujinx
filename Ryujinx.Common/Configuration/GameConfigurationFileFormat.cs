using Ryujinx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Configuration
{
    public class GameConfigurationFileFormat : ConfigurationFileFormat
    {
        /// <summary>
        /// The current version of the file format
        /// </summary>
        public new const int CurrentVersion = 23;

        /// <summary>
        /// Game-Specific Configuration Overrides
        /// </summary>
        public HashSet<string> Overrides { get; set; }

    }
}
