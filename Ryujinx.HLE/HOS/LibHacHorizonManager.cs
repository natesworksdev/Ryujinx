using LibHac;
using LibHac.Bcat;
using LibHac.FsSrv.Impl;
using LibHac.Loader;
using LibHac.Ncm;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Arp;
using System;
using StorageId = LibHac.Ncm.StorageId;

namespace Ryujinx.HLE.HOS
{
    public class LibHacHorizonManager
    {
        private LibHac.Horizon Server { get; set; }
        public HorizonClient RyujinxClient { get; private set; }

        public HorizonClient ApplicationClient { get; private set; }

        public HorizonClient BcatClient { get; private set; }
        public HorizonClient FsClient { get; private set; }
        public HorizonClient SdbClient { get; private set; }

        public LibHacHorizonManager()
        {
            InitializeServer();
        }

        private void InitializeServer()
        {
            Server = new LibHac.Horizon(new HorizonConfiguration());

            RyujinxClient = Server.CreatePrivilegedHorizonClient();
        }

        public void InitializeArpServer(Horizon system)
        {
            RyujinxClient.Sm.RegisterService(new LibHacArpServiceObject(new LibHacIReader(system)), "arp:r").ThrowIfFailure();
        }

        public void InitializeBcatServer()
        {
            BcatClient = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Bcat, StorageId.BuiltInSystem),
                BcatFsPermissions);

            _ = new BcatServer(BcatClient);
        }

        public void InitializeFsServer(VirtualFileSystem virtualFileSystem)
        {
            virtualFileSystem.InitializeFsServer(Server, out var fsClient);

            FsClient = fsClient;
        }

        public void InitializeSystemClients()
        {
            SdbClient = Server.CreateHorizonClient(new ProgramLocation(SystemProgramId.Sdb, StorageId.BuiltInSystem),
                SdbFacData, SdbFacDescriptor);
        }

        public void InitializeApplicationClient(ProgramId programId, in Npdm npdm)
        {
            ApplicationClient = Server.CreateHorizonClient(new ProgramLocation(programId, StorageId.BuiltInUser),
                npdm.FsAccessControlData, npdm.FsAccessControlDescriptor);
        }

        private AccessControlBits.Bits BcatFsPermissions => AccessControlBits.Bits.SystemSaveData;

        private ReadOnlySpan<byte> SdbFacData => new byte[]
        {
            0x01, 0x00, 0x00, 0x00, 0x08, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x1C, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,
            0x03, 0x03, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x09, 0x10, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x01
        };

        private ReadOnlySpan<byte> SdbFacDescriptor => new byte[]
        {
            0x01, 0x00, 0x02, 0x00, 0x08, 0x00, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x09, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
        };
    }
}
