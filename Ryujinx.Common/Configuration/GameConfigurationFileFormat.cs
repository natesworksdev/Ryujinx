using Ryujinx.Common.Utilities;
using Ryujinx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
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
        public new const int CurrentVersion = 1;

        /// <summary>
        /// Game-Specific Configuration Overrides
        /// </summary>
        public HashSet<string> Overrides { get; set; }

        /// <summary>
        /// Loads a configuration file from disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public static bool TryLoad(string path, out GameConfigurationFileFormat configurationFileFormat)
        {
            try
            {
                configurationFileFormat = JsonHelper.DeserializeFromFile<GameConfigurationFileFormat>(path);

                return true;
            }
            catch
            {
                configurationFileFormat = null;

                return false;
            }
        }

        /// <summary>
        /// Save a configuration file to disk
        /// </summary>
        /// <param name="path">The path to the JSON configuration file</param>
        public new void SaveConfig(string path)
        {
            using FileStream fileStream = File.Create(path, 4096, FileOptions.WriteThrough);
            JsonHelper.Serialize(fileStream, this, true);
        }
    }
}
