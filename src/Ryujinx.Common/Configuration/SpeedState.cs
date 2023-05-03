using Ryujinx.Common.Utilities;
using System;
using System.Text.Json.Serialization;

namespace Ryujinx.Common.Configuration
{
    [JsonConverter(typeof(TypedStringEnumConverter<SpeedState>))]
    [Flags]
    public enum SpeedState
    {
        Normal = 0,
        FastForward = 1 << 0,
        Turbo = 1 << 1
    }
}