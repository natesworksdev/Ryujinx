using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.FspSrv
{
    internal class IDirectory : IpcService, IDisposable
    {
        private const int DirectoryEntrySize = 0x310;

        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private List<string> _directoryEntries;

        private int _currentItemIndex;

        public event EventHandler<EventArgs> Disposed;

        public string HostPath { get; private set; }

        public IDirectory(string hostPath, int flags)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0, Read          },
                { 1, GetEntryCount }
            };

            HostPath = hostPath;

            _directoryEntries = new List<string>();

            if ((flags & 1) != 0) _directoryEntries.AddRange(Directory.GetDirectories(hostPath));

            if ((flags & 2) != 0) _directoryEntries.AddRange(Directory.GetFiles(hostPath));

            _currentItemIndex = 0;
        }

        public long Read(ServiceCtx context)
        {
            long bufferPosition = context.Request.ReceiveBuff[0].Position;
            long bufferLen      = context.Request.ReceiveBuff[0].Size;

            int maxReadCount = (int)(bufferLen / DirectoryEntrySize);

            int count = Math.Min(_directoryEntries.Count - _currentItemIndex, maxReadCount);

            for (int index = 0; index < count; index++)
            {
                long position = bufferPosition + index * DirectoryEntrySize;

                WriteDirectoryEntry(context, position, _directoryEntries[_currentItemIndex++]);
            }

            context.ResponseData.Write((long)count);

            return 0;
        }

        private void WriteDirectoryEntry(ServiceCtx context, long position, string fullPath)
        {
            for (int offset = 0; offset < 0x300; offset += 8) context.Memory.WriteInt64(position + offset, 0);

            byte[] nameBuffer = Encoding.UTF8.GetBytes(Path.GetFileName(fullPath));

            context.Memory.WriteBytes(position, nameBuffer);

            int  type = 0;
            long size = 0;

            if (File.Exists(fullPath))
            {
                type = 1;
                size = new FileInfo(fullPath).Length;
            }

            context.Memory.WriteInt32(position + 0x300, 0); //Padding?
            context.Memory.WriteInt32(position + 0x304, type);
            context.Memory.WriteInt64(position + 0x308, size);
        }

        public long GetEntryCount(ServiceCtx context)
        {
            context.ResponseData.Write((long)_directoryEntries.Count);

            return 0;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) Disposed?.Invoke(this, EventArgs.Empty);
        }
    }
}
