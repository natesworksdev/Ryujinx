using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Common.Configuration
{
    interface IConfigurationFileFormat
    {
        /// <summary>
        /// Loads a configuration file from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public abstract bool TryLoad(string path, out IConfigurationFileFormat configurationFileFormat);
    }
}
