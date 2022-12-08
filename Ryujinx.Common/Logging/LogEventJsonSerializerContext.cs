using System.Text.Json.Serialization;

namespace Ryujinx.Common.Logging;

[JsonSerializable(typeof(LogEventArgs))]
internal partial class LogEventJsonSerializerContext : JsonSerializerContext
{
}