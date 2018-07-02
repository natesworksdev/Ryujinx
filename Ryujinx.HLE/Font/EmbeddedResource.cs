using System.IO;
using System.Reflection;

namespace Ryujinx.HLE.Font
{
    static class EmbeddedResource
    {
        public static byte[] GetData(string Name)
        {
            Assembly Asm = typeof(EmbeddedResource).Assembly;

            using (Stream ResStream = Asm.GetManifestResourceStream(Name))
            {
                BinaryReader Reader = new BinaryReader(ResStream);
                return Reader.ReadBytes((int)Reader.BaseStream.Length);
            }
        }
    }
}
