using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ui.Common.Configuration
{
    public class ConfigurationStateManager
    {

        public static string ApplicationTitle { get; set; }
        public static string ApplicationId { get; set; }

        public static ConfigurationState Instance
        {
            get
            {
                return ConfigurationState.GameInstance ?? ConfigurationState.Instance;
            }
        }
        public static ConfigurationState.LoggerSection Logger
        {
            get
            {
                return Instance.Logger;
            }
        }
        public static ConfigurationState.HidSection Hid
        {
            get
            {
                return Instance.Hid;
            }
        }
        public static ConfigurationState.GraphicsSection Graphics
        {
            get
            {
                return Instance.Graphics;
            }
        }
        public static ConfigurationState.SystemSection System
        {
            get
            {
                return Instance.System;
            }
        }
        public static bool IsGameConfiguration
        {
            get
            {
                return Instance == ConfigurationState.GameInstance;
            }
        }
        public static string ConfigPathForApplication(string applicationId)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{applicationId}.json");
        }
    }
}
