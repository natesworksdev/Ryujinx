using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Ryujinx.HLE.Loaders.Npdm
{
    public class ServiceAccessControl
    {
        public List<Tuple<string, bool>> Services = new List<Tuple<string, bool>>();

        public ServiceAccessControl(Stream ServiceAccessControlStream, int Offset, int Size)
        {
            ServiceAccessControlStream.Seek(Offset, SeekOrigin.Begin);

            BinaryReader Reader = new BinaryReader(ServiceAccessControlStream);

            int ByteReaded = 0;

            while (ByteReaded != Size)
            {
                byte ControlByte = Reader.ReadByte();

                if (ControlByte == 0x00) break;

                int Length             = ((ControlByte & 0x07)) + 1;
                bool RegisterAllowed   = ((ControlByte & 0x80) != 0);
                byte[] TempServiceName = Reader.ReadBytes(Length);

                Services.Add(Tuple.Create(Encoding.ASCII.GetString(TempServiceName, 0, TempServiceName.Length), RegisterAllowed));

                ByteReaded += Length + 1;
            }
        }
    }
}
