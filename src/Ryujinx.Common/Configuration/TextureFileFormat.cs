using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration
{
    [JsonConverter(typeof(TypedStringEnumConverter<TextureFileFormat>))]
    public enum TextureFileFormat
    {
        Dds,
        Png,
    }
}
