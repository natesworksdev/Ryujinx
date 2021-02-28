using Ryujinx.Configuration;
using Ryujinx.Configuration.System;

namespace Ryujinx.Common.Configuration.ConfigurationStateSection
{
    /// <summary>
    /// System configuration section
    /// </summary>
    public class SystemSection
    {
        /// <summary>
        /// Change System Language
        /// </summary>
        public ReactiveObject<Language> Language { get; }

        /// <summary>
        /// Change System Region
        /// </summary>
        public ReactiveObject<Region> Region { get; }

        /// <summary>
        /// Change System TimeZone
        /// </summary>
        public ReactiveObject<string> TimeZone { get; }

        /// <summary>
        /// System Time Offset in Seconds
        /// </summary>
        public ReactiveObject<long> SystemTimeOffset { get; }

        /// <summary>
        /// Enables or disables Docked Mode
        /// </summary>
        public ReactiveObject<bool> EnableDockedMode { get; }

        /// <summary>
        /// Enables or disables profiled translation cache persistency
        /// </summary>
        public ReactiveObject<bool> EnablePtc { get; }

        /// <summary>
        /// Enables integrity checks on Game content files
        /// </summary>
        public ReactiveObject<bool> EnableFsIntegrityChecks { get; }

        /// <summary>
        /// Enables FS access log output to the console. Possible modes are 0-3
        /// </summary>
        public ReactiveObject<int> FsGlobalAccessLogMode { get; }

        /// <summary>
        /// The selected audio backend
        /// </summary>
        public ReactiveObject<AudioBackend> AudioBackend { get; }

        /// <summary>
        /// Enable or disable ignoring missing services
        /// </summary>
        public ReactiveObject<bool> IgnoreMissingServices { get; }

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
}
