using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Ryujinx.Ui.Common.Configuration
{
    public class ConfigurationStateManager
    {
        public static ConfigurationState.LoggerSection LoggerSection
        public static string ApplicationTitle { get; set; }
        public static string ApplicationId { get; set; }

        public static bool UseGameConfiguration { get; set; }

        public static ConfigurationState Instance
        {
            get
            {
                return UseGameConfiguration ? ConfigurationState.GameInstance : ConfigurationState.Instance;
            }
        }
        public static ConfigurationState GameInstance
        {
            get
            {
                return ConfigurationState.GameInstance;
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
        /*
         *  Graphics
         */
        public static ReactiveObject<BackendThreading?> BackendThreading
        {
            get
            {
                if (GameInstance?.Graphics.BackendThreading.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.BackendThreading;
                }
                return GameInstance.Graphics.BackendThreading;
            }
        }
        public static ReactiveObject<int?> ResScale
        {
            get
            {
                if (GameInstance?.Graphics.ResScale.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.ResScale;
                }
                return GameInstance.Graphics.ResScale;
            }
        }
        public static ReactiveObject<float?> ResScaleCustom
        {
            get
            {
                if (GameInstance?.Graphics.ResScaleCustom.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.ResScaleCustom;
                }
                return GameInstance.Graphics.ResScaleCustom;
            }
        }
        public static ReactiveObject<float?> MaxAnisotropy
        {
            get
            {
                if (GameInstance?.Graphics.MaxAnisotropy.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.MaxAnisotropy;
                }
                return GameInstance.Graphics.MaxAnisotropy;
            }
        }
        public static ReactiveObject<AspectRatio?> AspectRatio
        {
            get
            {
                if (GameInstance?.Graphics.AspectRatio.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.AspectRatio;
                }
                return GameInstance.Graphics.AspectRatio;
            }
        }
        public static ReactiveObject<string> ShadersDumpPath
        {
            get
            {
                if (GameInstance?.Graphics.ShadersDumpPath.Value == null || GameInstance.Graphics.ShadersDumpPath.Value.Equals(String.Empty))
                {
                    return ConfigurationState.Instance.Graphics.ShadersDumpPath;
                }
                return GameInstance.Graphics.ShadersDumpPath;
            }
        }
        public static ReactiveObject<bool?> EnableVsync
        {
            get
            {
                if (GameInstance?.Graphics.EnableVsync.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.EnableVsync;
                }
                return GameInstance.Graphics.EnableVsync;
            }
        }
        public static ReactiveObject<bool?> EnableShaderCache
        {
            get
            {
                if (GameInstance?.Graphics.EnableShaderCache.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.EnableShaderCache;
                }
                return GameInstance.Graphics.EnableShaderCache;
            }
        }
        public static ReactiveObject<bool?> EnableTextureRecompression
        {
            get
            {
                if (GameInstance?.Graphics.EnableTextureRecompression.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.EnableTextureRecompression;
                }
                return GameInstance.Graphics.EnableTextureRecompression;
            }
        }
        public static ReactiveObject<GraphicsBackend?> GraphicsBackend
        {
            get
            {
                if (GameInstance?.Graphics.GraphicsBackend.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.GraphicsBackend;
                }
                return GameInstance.Graphics.GraphicsBackend;
            }
        }
        public static ReactiveObject<string> PreferredGpu
        {
            get
            {
                if (GameInstance?.Graphics.PreferredGpu.Value == null || GameInstance.Graphics.PreferredGpu.Value.Equals(""))
                {
                    return ConfigurationState.Instance.Graphics.PreferredGpu;
                }
                return GameInstance.Graphics.PreferredGpu;
            }
        }
        public static ReactiveObject<bool?> EnableMacroHLE
        {
            get
            {
                if (GameInstance?.Graphics.EnableMacroHLE.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.EnableMacroHLE;
                }
                return GameInstance.Graphics.EnableMacroHLE;
            }
        }
        public static ReactiveObject<AntiAliasing?> AntiAliasing
        {
            get
            {
                if (GameInstance?.Graphics.AntiAliasing.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.AntiAliasing;
                }
                return GameInstance.Graphics.AntiAliasing;
            }
        }
        public static ReactiveObject<ScalingFilter?> ScalingFilter
        {
            get
            {
                if (GameInstance?.Graphics.ScalingFilter.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.ScalingFilter;
                }
                return GameInstance.Graphics.ScalingFilter;
            }
        }
        public static ReactiveObject<int?> ScalingFilterLevel
        {
            get
            {
                return ConfigurationState.GameInstance.Logger ?? ConfigurationState.Instance.Logger;
                if (GameInstance?.Graphics.ScalingFilterLevel.Value == null)
                {
                    return ConfigurationState.Instance.Graphics.ScalingFilterLevel;
                }
                return GameInstance.Graphics.ScalingFilterLevel;
            }
        }
        public static ConfigurationState.HidSection HidSection
        /*
         *  System
         */
        public static ConfigurationState.SystemSection System
        {
            get
            {
                return ConfigurationState.GameInstance.Hid ?? ConfigurationState.Instance.Hid;
                return Instance.System;
            }
        }
        public static ConfigurationState.GraphicsSection GraphicsSection
        public static bool IsGameConfiguration
        {
            get
            {
                return ConfigurationState.GameInstance.Graphics ?? ConfigurationState.Instance.Graphics;
                return Instance == ConfigurationState.GameInstance;
            }
        }

        public static void InitializeGameConfiguration(string applicationTitle, string applicationId)
        {
            if (ApplicationId == null) return;

            ConfigurationState.GameInstance = null;
            string applicationConfigurationPath = ConfigurationStateManager.ConfigPathForApplication(applicationId);
            ConfigurationStateManager.ApplicationTitle = applicationTitle;
            ConfigurationStateManager.ApplicationId = applicationId;

            ConfigurationState.InitializeGameConfig();
            ConfigurationFileFormat.TryLoad(applicationConfigurationPath, out ConfigurationFileFormat applicationConfigurationFileFormat);

            // Default to all global values
            if (applicationConfigurationFileFormat == null)
            {
                //ConfigurationFileFormat.TryLoad(Program.ConfigurationPath, out ConfigurationFileFormat globalConfigurationFileFormat);
                //ConfigurationLoadResult result = ConfigurationState.GameInstance.Load(globalConfigurationFileFormat, Program.ConfigurationPath);

                //if (result == ConfigurationLoadResult.NotLoaded)
                //{
                ConfigurationState.ResetGameConfig();
                //}
            }
            else
            {
                ConfigurationLoadResult result = ConfigurationState.GameInstance.Load(applicationConfigurationFileFormat, applicationConfigurationPath);

                if (result == ConfigurationLoadResult.NotLoaded)
                {
                    ConfigurationState.ResetGameConfig();
                }
            }
        }
        public static string ConfigPathForApplication(string applicationId)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{applicationId}.json");
        }
    }
}
