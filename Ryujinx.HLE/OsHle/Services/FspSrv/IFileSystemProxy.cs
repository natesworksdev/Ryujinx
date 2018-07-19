using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle.Ipc;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.OsHle.Services.FspSrv
{
    class IFileSystemProxy : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public IFileSystemProxy()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 1,    SetCurrentProcess                    },
                { 18,   OpenSdCardFileSystem                 },
                { 22,   CreateSaveDataFileSystem             },
                { 51,   OpenSaveDataFileSystem               },
                { 200,  OpenDataStorageByCurrentProcess      },
                { 202,  OpenDataStorageByDataId              },
                { 203,  OpenPatchDataStorageByCurrentProcess },
                { 1005, GetGlobalAccessLogMode               }
            };
        }
        public long SetCurrentProcess(ServiceCtx Context)
        {
            return 0;
        }

        public long OpenSdCardFileSystem(ServiceCtx Context)
        {
            MakeObject(Context, new IFileSystem(Context.Ns.VFs.GetSdCardPath()));

            return 0;
        }

        public long CreateSaveDataFileSystem(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceFs, "Stubbed.");

            return 0;
        }

        public long OpenSaveDataFileSystem(ServiceCtx Context)
        {
            MakeObject(Context, new IFileSystem(Context.Ns.VFs.GetGameSavesPath()));

            return 0;
        }

        public long OpenDataStorageByCurrentProcess(ServiceCtx Context)
        {
            MakeObject(Context, new IStorage(Context.Ns.VFs.RomFs));

            return 0;
        }

        public long OpenDataStorageByDataId(ServiceCtx Context)
        {
            StorageTypes StorageId = (StorageTypes) Context.RequestData.ReadByte();
            long         TitleId   = Context.RequestData.ReadInt64();

            if (StorageId == StorageTypes.NandSystem)
            {
                Context.Ns.Log.PrintStub(LogClass.ServiceFs, $"[{StorageId}] Stubbed.");

                Stream FakeStream = new MemoryStream();

                MakeObject(Context, new IStorage(FakeStream));
            }
            else
            {
                Context.Ns.Log.PrintWarning(LogClass.ServiceFs, $"{StorageId} is not implemented!");

                Stream FakeStream = new MemoryStream();

                MakeObject(Context, new IStorage(FakeStream));
            }

            return 0;
        }

        public long OpenPatchDataStorageByCurrentProcess(ServiceCtx Context)
        {
            MakeObject(Context, new IStorage(Context.Ns.VFs.RomFs));

            return 0;
        }

        public long GetGlobalAccessLogMode(ServiceCtx Context)
        {
            Context.ResponseData.Write(0);

            return 0;
        }
    }
}