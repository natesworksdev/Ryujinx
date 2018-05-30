using Ryujinx.Core.OsHle.Ipc;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.Core.OsHle.Services.Am
{
    class ILibraryAppletCreator : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public ILibraryAppletCreator()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,  CreateLibraryApplet },
                { 10, CreateStorage       }
            };
        }

        private const uint LaunchParamsMagic = 0xc79497ca;

        public long CreateLibraryApplet(ServiceCtx Context)
        {
            MakeObject(Context, new ILibraryAppletAccessor());

            return 0;
        }

        public long CreateStorage(ServiceCtx Context)
        {
            MakeObject(Context, new IStorage(MakeLaunchParams()));

            return 0;
        }

        private byte[] MakeLaunchParams()
        {
            //Size needs to be at least 0x88 bytes otherwise application errors.
            using (MemoryStream MS = new MemoryStream())
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                MS.SetLength(0x88);

                Writer.Write(LaunchParamsMagic);
                Writer.Write(1);  //IsAccountSelected? Only lower 8 bits actually used.
                Writer.Write(1L); //User Id Low (note: User Id needs to be != 0)
                Writer.Write(0L); //User Id High

                return MS.ToArray();
            }
        }
    }
}