using System.Collections.Generic;
using Ryujinx.Common.Configuration.Hid;
using Ryujinx.Common.Logging;
using Ryujinx.Configuration.System;
using Ryujinx.Configuration;

namespace Ryujinx.Common.Configuration
{
    public interface IConfigurationState
    {
        /// <summary>
        /// Logger configuration section
        /// </summary>
        public class LoggerSection
        {
            /// <summary>
            /// Enables printing debug log messages
            /// </summary>
            public ReactiveObject<bool> EnableDebug { get; protected set; }

            /// <summary>
            /// Enables printing stub log messages
            /// </summary>
            public ReactiveObject<bool> EnableStub { get; protected set; }

            /// <summary>
            /// Enables printing info log messages
            /// </summary>
            public ReactiveObject<bool> EnableInfo { get; protected set; }

            /// <summary>
            /// Enables printing warning log messages
            /// </summary>
            public ReactiveObject<bool> EnableWarn { get; protected set; }

            /// <summary>
            /// Enables printing error log messages
            /// </summary>
            public ReactiveObject<bool> EnableError { get; protected set; }

            /// <summary>
            /// Enables printing guest log messages
            /// </summary>
            public ReactiveObject<bool> EnableGuest { get; protected set; }

            /// <summary>
            /// Enables printing FS access log messages
            /// </summary>
            public ReactiveObject<bool> EnableFsAccessLog { get; protected set; }

            /// <summary>
            /// Controls which log messages are written to the log targets
            /// </summary>
            public ReactiveObject<LogClass[]> FilteredClasses { get; protected set; }

            /// <summary>
            /// Enables or disables logging to a file on disk
            /// </summary>
            public ReactiveObject<bool> EnableFileLog { get; protected set; }

            /// <summary>
            /// Controls which OpenGL log messages are recorded in the log
            /// </summary>
            public ReactiveObject<GraphicsDebugLevel> GraphicsDebugLevel { get; protected set; }

            public LoggerSection()
            {
                EnableDebug = new ReactiveObject<bool>();
                EnableStub = new ReactiveObject<bool>();
                EnableInfo = new ReactiveObject<bool>();
                EnableWarn = new ReactiveObject<bool>();
                EnableError = new ReactiveObject<bool>();
                EnableGuest = new ReactiveObject<bool>();
                EnableFsAccessLog = new ReactiveObject<bool>();
                FilteredClasses = new ReactiveObject<LogClass[]>();
                EnableFileLog = new ReactiveObject<bool>();
                GraphicsDebugLevel = new ReactiveObject<GraphicsDebugLevel>();
            }
        }

        /// <summary>
        /// System configuration section
        /// </summary>
        public class SystemSection
        {
            /// <summary>
            /// Change System Language
            /// </summary>
            public ReactiveObject<Language> Language { get; protected set; }

            /// <summary>
            /// Change System Region
            /// </summary>
            public ReactiveObject<Region> Region { get; protected set; }

            /// <summary>
            /// Change System TimeZone
            /// </summary>
            public ReactiveObject<string> TimeZone { get; protected set; }

            /// <summary>
            /// System Time Offset in Seconds
            /// </summary>
            public ReactiveObject<long> SystemTimeOffset { get; protected set; }

            /// <summary>
            /// Enables or disables Docked Mode
            /// </summary>
            public ReactiveObject<bool> EnableDockedMode { get; protected set; }

            /// <summary>
            /// Enables or disables profiled translation cache persistency
            /// </summary>
            public ReactiveObject<bool> EnablePtc { get; protected set; }

            /// <summary>
            /// Enables integrity checks on Game content files
            /// </summary>
            public ReactiveObject<bool> EnableFsIntegrityChecks { get; protected set; }

            /// <summary>
            /// Enables FS access log output to the console. Possible modes are 0-3
            /// </summary>
            public ReactiveObject<int> FsGlobalAccessLogMode { get; protected set; }

            /// <summary>
            /// The selected audio backend
            /// </summary>
            public ReactiveObject<AudioBackend> AudioBackend { get; protected set; }

            /// <summary>
            /// Enable or disable ignoring missing services
            /// </summary>
            public ReactiveObject<bool> IgnoreMissingServices { get; protected set; }

            public SystemSection()
            {
                Language = new ReactiveObject<Language>();
                Region = new ReactiveObject<Region>();
                TimeZone = new ReactiveObject<string>();
                SystemTimeOffset = new ReactiveObject<long>();
                EnableDockedMode = new ReactiveObject<bool>();
                EnablePtc = new ReactiveObject<bool>();
                EnableFsIntegrityChecks = new ReactiveObject<bool>();
                FsGlobalAccessLogMode = new ReactiveObject<int>();
                AudioBackend = new ReactiveObject<AudioBackend>();
                IgnoreMissingServices = new ReactiveObject<bool>();
            }
        }

        /// <summary>
        /// Hid configuration section
        /// </summary>
        public class HidSection
        {
            /// <summary>
            /// Enable or disable keyboard support (Independent from controllers binding)
            /// </summary>
            public ReactiveObject<bool> EnableKeyboard { get; protected set; }

            /// <summary>
            /// Hotkey Keyboard Bindings
            /// </summary>
            public ReactiveObject<KeyboardHotkeys> Hotkeys { get; protected set; }

            /// <summary>
            /// Input device configuration.
            /// NOTE: This ReactiveObject won't issue an event when the List has elements added or removed.
            /// TODO: Implement a ReactiveList class.
            /// </summary>
            public ReactiveObject<List<InputConfig>> InputConfig { get; protected set; }

            public HidSection()
            {
                EnableKeyboard = new ReactiveObject<bool>();
                Hotkeys = new ReactiveObject<KeyboardHotkeys>();
                InputConfig = new ReactiveObject<List<InputConfig>>();
            }
        }

        /// <summary>
        /// Graphics configuration section
        /// </summary>
        public class GraphicsSection
        {
            /// <summary>
            /// Max Anisotropy. Values range from 0 - 16. Set to -1 to let the game decide.
            /// </summary>
            public ReactiveObject<float> MaxAnisotropy { get; protected set; }

            /// <summary>
            /// Aspect Ratio applied to the renderer window.
            /// </summary>
            public ReactiveObject<AspectRatio> AspectRatio { get; protected set; }

            /// <summary>
            /// Resolution Scale. An integer scale applied to applicable render targets. Values 1-4, or -1 to use a custom floating point scale instead.
            /// </summary>
            public ReactiveObject<int> ResScale { get; protected set; }

            /// <summary>
            /// Custom Resolution Scale. A custom floating point scale applied to applicable render targets. Only active when Resolution Scale is -1.
            /// </summary>
            public ReactiveObject<float> ResScaleCustom { get; protected set; }

            /// <summary>
            /// Dumps shaders in this local directory
            /// </summary>
            public ReactiveObject<string> ShadersDumpPath { get; protected set; }

            /// <summary>
            /// Enables or disables Vertical Sync
            /// </summary>
            public ReactiveObject<bool> EnableVsync { get; protected set; }

            /// <summary>
            /// Enables or disables Shader cache
            /// </summary>
            public ReactiveObject<bool> EnableShaderCache { get; protected set; }

            public GraphicsSection()
            {
                ResScale = new ReactiveObject<int>();
                ResScaleCustom = new ReactiveObject<float>();
                MaxAnisotropy = new ReactiveObject<float>();
                AspectRatio = new ReactiveObject<AspectRatio>();
                ShadersDumpPath = new ReactiveObject<string>();
                EnableVsync = new ReactiveObject<bool>();
                EnableShaderCache = new ReactiveObject<bool>();
            }
        }

    }
}

