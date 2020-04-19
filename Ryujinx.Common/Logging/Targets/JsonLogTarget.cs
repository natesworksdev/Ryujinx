using System.IO;
using Utf8Json;

namespace Ryujinx.Common.Logging
{
    public sealed class JsonLogTarget : ILogTarget
    {
        private readonly Stream _stream;
        private readonly bool   _leaveOpen;
        private readonly string _name;

        string ILogTarget.Name => _name;

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
            JsonSerializer.Serialize(_stream, e);
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
