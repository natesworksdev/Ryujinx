using System.Text.Json.Serialization;

namespace Ryujinx.Common.Logging;

[JsonSerializable(typeof(JsonLogEventArgs))]
internal partial class LogEventJsonSerializerContext : JsonSerializerContext
{
}