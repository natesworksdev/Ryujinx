using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.Utilities;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.FspSrv
{
    class FileSystemProxy : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public FileSystemProxy()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1,    SetCurrentProcess                        },
                { 18,   OpenSdCardFileSystem                     },
                { 51,   OpenSaveDataFileSystem                   },
                { 52,   OpenSaveDataFileSystemBySystemSaveDataId },
                { 200,  OpenDataStorageByCurrentProcess          },
                { 203,  OpenPatchDataStorageByCurrentProcess     },
                { 1005, GetGlobalAccessLogMode                   }
            };
        }

        public long SetCurrentProcess(ServiceCtx context)
        {
            return 0;
        }

        public long OpenSdCardFileSystem(ServiceCtx context)
        {
            MakeObject(context, new FileSystem(context.Device.FileSystem.GetSdCardPath()));

            return 0;
        }

        public long OpenSaveDataFileSystem(ServiceCtx context)
        {
            LoadSaveDataFileSystem(context);

            return 0;
        }

        public long OpenSaveDataFileSystemBySystemSaveDataId(ServiceCtx context)
        {
            LoadSaveDataFileSystem(context);

            return 0;
        }

        public long OpenDataStorageByCurrentProcess(ServiceCtx context)
        {
            MakeObject(context, new Storage(context.Device.FileSystem.RomFs));

            return 0;
        }

        public long OpenPatchDataStorageByCurrentProcess(ServiceCtx context)
        {
            MakeObject(context, new Storage(context.Device.FileSystem.RomFs));

            return 0;
        }

        public long GetGlobalAccessLogMode(ServiceCtx context)
        {
            context.ResponseData.Write(0);

            return 0;
        }

        public void LoadSaveDataFileSystem(ServiceCtx context)
        {
            SaveSpaceId saveSpaceId = (SaveSpaceId)context.RequestData.ReadInt64();

            long titleId = context.RequestData.ReadInt64();

            UInt128 userId = new UInt128(
                context.RequestData.ReadInt64(), 
                context.RequestData.ReadInt64());

            long saveId = context.RequestData.ReadInt64();

            SaveDataType saveDataType = (SaveDataType)context.RequestData.ReadByte();

            SaveInfo saveInfo = new SaveInfo(titleId, saveId, saveDataType, userId, saveSpaceId);

            MakeObject(context, new FileSystem(context.Device.FileSystem.GetGameSavePath(saveInfo, context)));
        }
    }
}