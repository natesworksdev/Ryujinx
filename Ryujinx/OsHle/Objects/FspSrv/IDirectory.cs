using ChocolArm64.Memory;
using Ryujinx.OsHle.Ipc;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ryujinx.OsHle.Objects.FspSrv
{
    struct DirectoryEntry
    {
        public string Name;
        public byte   Type;
        public long   Size;
    }

    class IDirectory : IIpcInterface
    {
        private List<DirectoryEntry> DirectoryEntries = new List<DirectoryEntry>();
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        private string HostPath;

        const int DirectoryEntryType_Directory = 0;
        const int DirectoryEntryType_File      = 1;
        public IDirectory(string HostPath, int flags)
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                {  0, Read          },
                {  1, GetEntryCount }
            };

            this.HostPath = HostPath;

            if ((flags & 1) == 1)
            {
                string[] Directories = Directory.GetDirectories(HostPath, "*", SearchOption.TopDirectoryOnly).
                             Where(x => (new FileInfo(x).Attributes & FileAttributes.Hidden) == 0).ToArray();
                             
                foreach (string Directory in Directories)
                {
                    DirectoryEntry Info = new DirectoryEntry
                    {
                        Name = Directory,
                        Type = DirectoryEntryType_Directory,
                        Size = 0
                    };
                    DirectoryEntries.Add(Info);
                }
            }

            if ((flags & 2) == 2)
            {
                string[] Files = Directory.GetFiles(HostPath, "*", SearchOption.TopDirectoryOnly).
                       Where(x => (new FileInfo(x).Attributes & FileAttributes.Hidden) == 0).ToArray();
                       
                foreach (string FileName in Files)
                {
                    DirectoryEntry Info = new DirectoryEntry
                    {
                        Name = Path.GetFileName(FileName),
                        Type = DirectoryEntryType_File,
                        Size = new FileInfo(Path.Combine(HostPath, FileName)).Length
                    };
                    DirectoryEntries.Add(Info);
                }
            }
        }

        private int LastItem = 0;
        const   int DirectoryEntrySize = 0x310;
        public long Read(ServiceCtx Context)
        {
            long BufferPosition = Context.Request.ReceiveBuff[0].Position;
            long BufferLen      = Context.Request.ReceiveBuff[0].Size;
            long MaxDirectories = BufferLen / DirectoryEntrySize;

            if (MaxDirectories >= DirectoryEntries.Count) MaxDirectories = DirectoryEntries.Count;

            int CurrentIndex, CurrentItem;
            byte[] DirectoryEntry = new byte[DirectoryEntrySize];
            for (CurrentIndex = 0, CurrentItem = LastItem; CurrentItem < MaxDirectories; CurrentIndex++, CurrentItem++)
            {
                MemoryStream MemStream = new MemoryStream();
                BinaryWriter Writer    = new BinaryWriter(MemStream);
                
                Writer.Write(Encoding.UTF8.GetBytes(DirectoryEntries[CurrentItem].Name));
                Writer.Seek(0x304, SeekOrigin.Begin);
                Writer.Write(DirectoryEntries[CurrentItem].Type);
                Writer.Seek(0x308, SeekOrigin.Begin);
                Writer.Write(DirectoryEntries[CurrentItem].Size);

                MemStream.Seek(0, SeekOrigin.Begin);
                MemStream.Read(DirectoryEntry, 0, 0x310);
                AMemoryHelper.WriteBytes(Context.Memory, BufferPosition + DirectoryEntrySize * CurrentIndex, DirectoryEntry);
            }

            if (LastItem < DirectoryEntries.Count)
            {
                LastItem = CurrentItem;
                Context.ResponseData.Write((long)CurrentIndex); // index = number of entries written this call.
            }
            else
            {
                Context.ResponseData.Write((long)0);
            }

            return 0;
        }

        public long GetEntryCount(ServiceCtx Context)
        {
            Context.ResponseData.Write((long)DirectoryEntries.Count);
            return 0;
        }
    }
}
