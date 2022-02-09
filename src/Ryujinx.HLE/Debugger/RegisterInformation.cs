using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.Debugger
{
    class RegisterInformation
    {
        public static readonly Dictionary<string, string> Features = new()
        {
            { "target.xml", GetEmbeddedResourceContent("target.xml") },
            { "aarch64-core.xml", GetEmbeddedResourceContent("aarch64-core.xml") },
            { "aarch64-fpu.xml", GetEmbeddedResourceContent("aarch64-fpu.xml") },
        };

        private static string GetEmbeddedResourceContent(string resourceName)
        {
            Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Ryujinx.HLE.Debugger.GdbXml." + resourceName);
            StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();
            reader.Dispose();
            stream.Dispose();
            return result;
        }
    }
}
