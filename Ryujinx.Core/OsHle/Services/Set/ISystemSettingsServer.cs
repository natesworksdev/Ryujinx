using ChocolArm64.Memory;
using Ryujinx.Core.OsHle.Ipc;
using Ryujinx.Core.Settings;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ryujinx.Core.OsHle.Services.Set
{
    class ISystemSettingsServer : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public ISystemSettingsServer()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                {  4, GetFirmwareVersion2 },
                { 23, GetColorSetId       },
                { 24, SetColorSetId       }
            };
        }

        public static long GetFirmwareVersion2(ServiceCtx Context)
        {
            long ReplyPos  = Context.Request.RecvListBuff[0].Position;
            long ReplySize = Context.Request.RecvListBuff[0].Size;

            //http://switchbrew.org/index.php?title=System_Version_Title
            using (MemoryStream MS = new MemoryStream(0x100))
            {
                BinaryWriter Writer = new BinaryWriter(MS);

                Writer.Write((byte)0x3); //Major FW Version
                Writer.Write((byte)0x0); //Minor FW Version
                Writer.Write((byte)0x0); //Micro FW Version
                Writer.Write((byte)0x0); //Unknown/Build?

                Writer.Write(0x0A); //Revision Number

                Writer.Write(Encoding.ASCII.GetBytes("NX"), 0, 0x02); //Platform String "NX"

                MS.Seek(0x28, SeekOrigin.Begin);
                Writer.Write(Encoding.ASCII.GetBytes("7fbde2b0bba4d14107bf836e4643043d9f6c8e47"), 0, 0x28); //Hex ASCII String

                MS.Seek(0x68, SeekOrigin.Begin);
                Writer.Write(Encoding.ASCII.GetBytes("3.0.0"), 0, 0x05); //System-Version

                MS.Seek(0x80, SeekOrigin.Begin);
                Writer.Write(Encoding.ASCII.GetBytes("NintendoSDK Firmware for NX 3.0.0-10.0"), 0, 0x26); //Build String

                AMemoryHelper.WriteBytes(Context.Memory, ReplyPos, MS.ToArray());
            }

            return 0;
        }

        public static long GetColorSetId(ServiceCtx Context)
        {
            Context.ResponseData.Write((int)Context.Ns.Settings.ThemeColor);

            return 0;
        }

        public static long SetColorSetId(ServiceCtx Context)
        {
            int ColorSetId = Context.RequestData.ReadInt32();

            Context.Ns.Settings.ThemeColor = (ColorSet)ColorSetId;
            return 0;
        }
    }
}