using System;
using System.IO;

namespace Ryujinx.Graphics.Gal
{
    public static class ShaderHelper
    {
        private static string RuntimeDir;

        private static int DumpIndex = 1;

        public static void Dump(byte[] Binary, GalShaderType Type, string ExtSuffix = "")
        {
            if (string.IsNullOrWhiteSpace(GraphicsConfig.ShadersDumpPath))
            {
                return;
            }

            string FileName = "Shader" + DumpIndex.ToString("d4") + "." + ShaderExtension(Type) + ExtSuffix + ".bin";

            string FullPath = Path.Combine(FullDir(), FileName);
            string CodePath = Path.Combine(CodeDir(), FileName);

            DumpIndex++;

            File.WriteAllBytes(FullPath, Binary);

            using (FileStream CodeFile = File.Create(CodePath))
            using (BinaryWriter Writer = new BinaryWriter(CodeFile))
            {
                long Offset;

                for (Offset = 0; Offset + 0x50 < Binary.LongLength; Offset++)
                {
                    Writer.Write(Binary[Offset + 0x50]);
                }

                //Align to meet nvdisasm requeriments
                while (Offset % 0x20 != 0)
                {
                    Writer.Write(0);

                    Offset += 4;
                }
            }
        }

        private static string FullDir()
        {
            return CreateAndReturn(Path.Combine(DumpDir(), "Full"));
        }

        private static string CodeDir()
        {
            return CreateAndReturn(Path.Combine(DumpDir(), "Code"));
        }

        private static string DumpDir()
        {
            if (string.IsNullOrEmpty(RuntimeDir))
            {
                int Index = 1;

                do
                {
                    RuntimeDir = Path.Combine(GraphicsConfig.ShadersDumpPath, "Dumps" + Index.ToString("d2"));

                    Index++;
                }
                while (Directory.Exists(RuntimeDir));

                Directory.CreateDirectory(RuntimeDir);
            }

            return RuntimeDir;
        }

        private static string CreateAndReturn(string Dir)
        {
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }

            return Dir;
        }

        private static string ShaderExtension(GalShaderType Type)
        {
            switch (Type)
            {
                case GalShaderType.Vertex:         return "vert";
                case GalShaderType.TessControl:    return "tesc";
                case GalShaderType.TessEvaluation: return "tese";
                case GalShaderType.Geometry:       return "geom";
                case GalShaderType.Fragment:       return "frag";

                default: throw new ArgumentException(nameof(Type));
            }
        }
    }
}