using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using static Ryujinx.HLE.HOS.ErrorCode;

namespace Ryujinx.HLE.HOS.Services.FspSrv
{
    class FileSystem : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        private HashSet<string> _openPaths;

        private string _path;

        public FileSystem(string path)
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  CreateFile                 },
                { 1,  DeleteFile                 },
                { 2,  CreateDirectory            },
                { 3,  DeleteDirectory            },
                { 4,  DeleteDirectoryRecursively },
                { 5,  RenameFile                 },
                { 6,  RenameDirectory            },
                { 7,  GetEntryType               },
                { 8,  OpenFile                   },
                { 9,  OpenDirectory              },
                { 10, Commit                     },
                { 11, GetFreeSpaceSize           },
                { 12, GetTotalSpaceSize          },
                { 13, CleanDirectoryRecursively  },
                //{ 14, GetFileTimeStampRaw        }
            };

            _openPaths = new HashSet<string>();

            _path = path;
        }

        public long CreateFile(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            long mode = context.RequestData.ReadInt64();
            int  size = context.RequestData.ReadInt32();

            string fileName = context.Device.FileSystem.GetFullPath(_path, name);

            if (fileName == null)
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (System.IO.File.Exists(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyExists);
            }

            if (IsPathAlreadyInUse(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            using (FileStream newFile = System.IO.File.Create(fileName))
            {
                newFile.SetLength(size);
            }

            return 0;
        }

        public long DeleteFile(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            string fileName = context.Device.FileSystem.GetFullPath(_path, name);

            if (!System.IO.File.Exists(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (IsPathAlreadyInUse(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            System.IO.File.Delete(fileName);

            return 0;
        }

        public long CreateDirectory(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            string dirName = context.Device.FileSystem.GetFullPath(_path, name);

            if (dirName == null)
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (System.IO.Directory.Exists(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyExists);
            }

            if (IsPathAlreadyInUse(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            System.IO.Directory.CreateDirectory(dirName);

            return 0;
        }

        public long DeleteDirectory(ServiceCtx context)
        {
            return DeleteDirectory(context, false);
        }

        public long DeleteDirectoryRecursively(ServiceCtx context)
        {
            return DeleteDirectory(context, true);
        }

        private long DeleteDirectory(ServiceCtx context, bool recursive)
        {
            string name = ReadUtf8String(context);

            string dirName = context.Device.FileSystem.GetFullPath(_path, name);

            if (!System.IO.Directory.Exists(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (IsPathAlreadyInUse(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            System.IO.Directory.Delete(dirName, recursive);

            return 0;
        }

        public long RenameFile(ServiceCtx context)
        {
            string oldName = ReadUtf8String(context, 0);
            string newName = ReadUtf8String(context, 1);

            string oldFileName = context.Device.FileSystem.GetFullPath(_path, oldName);
            string newFileName = context.Device.FileSystem.GetFullPath(_path, newName);

            if (!System.IO.File.Exists(oldFileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (System.IO.File.Exists(newFileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyExists);
            }

            if (IsPathAlreadyInUse(oldFileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            System.IO.File.Move(oldFileName, newFileName);

            return 0;
        }

        public long RenameDirectory(ServiceCtx context)
        {
            string oldName = ReadUtf8String(context, 0);
            string newName = ReadUtf8String(context, 1);

            string oldDirName = context.Device.FileSystem.GetFullPath(_path, oldName);
            string newDirName = context.Device.FileSystem.GetFullPath(_path, newName);

            if (!System.IO.Directory.Exists(oldDirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (System.IO.Directory.Exists(newDirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyExists);
            }

            if (IsPathAlreadyInUse(oldDirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            System.IO.Directory.Move(oldDirName, newDirName);

            return 0;
        }

        public long GetEntryType(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            string fileName = context.Device.FileSystem.GetFullPath(_path, name);

            if (System.IO.File.Exists(fileName))
            {
                context.ResponseData.Write(1);
            }
            else if (System.IO.Directory.Exists(fileName))
            {
                context.ResponseData.Write(0);
            }
            else
            {
                context.ResponseData.Write(0);

                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            return 0;
        }

        public long OpenFile(ServiceCtx context)
        {
            int filterFlags = context.RequestData.ReadInt32();

            string name = ReadUtf8String(context);

            string fileName = context.Device.FileSystem.GetFullPath(_path, name);

            if (!System.IO.File.Exists(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (IsPathAlreadyInUse(fileName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            FileStream stream = new FileStream(fileName, FileMode.Open);

            File fileInterface = new File(stream, fileName);

            fileInterface.Disposed += RemoveFileInUse;

            lock (_openPaths)
            {
                _openPaths.Add(fileName);
            }

            MakeObject(context, fileInterface);

            return 0;
        }

        public long OpenDirectory(ServiceCtx context)
        {
            int filterFlags = context.RequestData.ReadInt32();

            string name = ReadUtf8String(context);

            string dirName = context.Device.FileSystem.GetFullPath(_path, name);

            if (!System.IO.Directory.Exists(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            Directory dirInterface = new Directory(dirName, filterFlags);

            dirInterface.Disposed += RemoveDirectoryInUse;

            lock (_openPaths)
            {
                _openPaths.Add(dirName);
            }

            MakeObject(context, dirInterface);

            return 0;
        }

        public long Commit(ServiceCtx context)
        {
            return 0;
        }

        public long GetFreeSpaceSize(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            context.ResponseData.Write(context.Device.FileSystem.GetDrive().AvailableFreeSpace);

            return 0;
        }

        public long GetTotalSpaceSize(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            context.ResponseData.Write(context.Device.FileSystem.GetDrive().TotalSize);

            return 0;
        }

        public long CleanDirectoryRecursively(ServiceCtx context)
        {
            string name = ReadUtf8String(context);

            string dirName = context.Device.FileSystem.GetFullPath(_path, name);

            if (!System.IO.Directory.Exists(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathDoesNotExist);
            }

            if (IsPathAlreadyInUse(dirName))
            {
                return MakeError(ErrorModule.Fs, FsErr.PathAlreadyInUse);
            }

            foreach (string entry in System.IO.Directory.EnumerateFileSystemEntries(dirName))
            {
                if (System.IO.Directory.Exists(entry))
                {
                    System.IO.Directory.Delete(entry, true);
                }
                else if (System.IO.File.Exists(entry))
                {
                    System.IO.File.Delete(entry);
                }
            }

            return 0;
        }

        private bool IsPathAlreadyInUse(string path)
        {
            lock (_openPaths)
            {
                return _openPaths.Contains(path);
            }
        }

        private void RemoveFileInUse(object sender, EventArgs e)
        {
            File fileInterface = (File)sender;

            lock (_openPaths)
            {
                fileInterface.Disposed -= RemoveFileInUse;

                _openPaths.Remove(fileInterface.HostPath);
            }
        }

        private void RemoveDirectoryInUse(object sender, EventArgs e)
        {
            Directory dirInterface = (Directory)sender;

            lock (_openPaths)
            {
                dirInterface.Disposed -= RemoveDirectoryInUse;

                _openPaths.Remove(dirInterface.HostPath);
            }
        }

        private string ReadUtf8String(ServiceCtx context, int index = 0)
        {
            long position = context.Request.PtrBuff[index].Position;
            long size     = context.Request.PtrBuff[index].Size;

            using (MemoryStream ms = new MemoryStream())
            {
                while (size-- > 0)
                {
                    byte value = context.Memory.ReadByte(position++);

                    if (value == 0)
                    {
                        break;
                    }

                    ms.WriteByte(value);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }
    }
}