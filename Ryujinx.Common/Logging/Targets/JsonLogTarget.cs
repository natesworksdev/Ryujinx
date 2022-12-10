using Ryujinx.Common.Utilities;
using System.IO;

namespace Ryujinx.Common.Logging
{
    public class JsonLogTarget : ILogTarget
    {
        private Stream _stream;
        private bool   _leaveOpen;
        private string _name;

        string ILogTarget.Name { get => _name; }

        public JsonLogTarget(Stream stream, string name)
        {
            _stream = stream;
            _name   = name;
        }

        public JsonLogTarget(Stream stream, bool leaveOpen)
        {
            _stream    = stream;
            _leaveOpen = leaveOpen;
        }

        public void Log(object sender, LogEventArgs e)
        {
            var jsonLogEventArgs = JsonLogEventArgs.FromLogEventArgs(e);
            string text = JsonHelper.Serialize(jsonLogEventArgs, LogEventJsonSerializerContext.Default.JsonLogEventArgs);

            using BinaryWriter writer = new(_stream);
            writer.Write(text);
        }

        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _stream.Dispose();
            }
        }
    }
}
