using Ryujinx.Common.Utilities;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration
{
    [JsonConverter(typeof(TypedStringEnumConverter<ShadingLanguage>))]
    public enum ShadingLanguage
    {
        SPIRV,
        GLSL,
    }
}
