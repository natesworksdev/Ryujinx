using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading;
using Mono.Unix;
using Ryujinx.Common.Logging;

namespace Ryujinx.Common.SystemInterop
{
    public class StdErrAdapter : IDisposable
    {
        private bool _disposable = false;
        private UnixPipes _stdErrPipe;
        private Thread _worker;

        public StdErrAdapter()
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                RegisterPosix();
            }
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void RegisterPosix()
        {
            const int stdErrFileno = 2;

            _stdErrPipe = UnixPipes.CreatePipes();
            Mono.Unix.Native.Syscall.dup2(_stdErrPipe.Writing.Handle, stdErrFileno);
            _worker = new Thread(EventWorker);
            _disposable = true;
            _worker.Start();
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private void EventWorker()
        {
            TextReader reader = new StreamReader(_stdErrPipe.Reading);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Logger.Error?.PrintRawMsg(line);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposable)
            {
                _stdErrPipe.Reading.Close();
                _stdErrPipe.Writing.Close();
                _stdErrPipe.Reading.Dispose();
                _stdErrPipe.Writing.Dispose();

                _disposable = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
