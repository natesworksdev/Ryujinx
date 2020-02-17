using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Ryujinx.Actions
{
    public class VersionCore
    {
        private static readonly Regex REGEX = new Regex(@"^(?<major>\d+)" +
                                                          @"(?>\.(?<minor>\d+))?" +
                                                          @"(?>\.(?<patch>\d+))?$",
            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public uint Major { get; private set; }
        public uint Minor { get; private set; }
        public uint Patch { get; private set; }

        public VersionCore(uint major, uint minor, uint patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public void IncrementPatch()
        {
            Patch++;
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        public static VersionCore FromString(string version)
        {
            Match match = REGEX.Match(version);

            if (!match.Success)
            {
                return null;
            }

            uint major = uint.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
            uint minor = uint.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture);
            uint patch = uint.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture);

            return new VersionCore(major, minor, patch);
        }

        public class Converter : JsonConverter<VersionCore>
        {
            public override VersionCore Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return FromString(reader.GetString());
            }

            public override void Write(Utf8JsonWriter writer, VersionCore value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
