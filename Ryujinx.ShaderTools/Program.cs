using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.IO;

namespace Ryujinx.ShaderTools
{
    class Program
    {
        private static readonly int MaxUboSize = 65536;

        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                //GlslDecompiler Decompiler = new GlslDecompiler(MaxUboSize);

                GalShaderType ShaderType = GalShaderType.Vertex;

                switch (args[0].ToLower())
                {
                    case "v":  ShaderType = GalShaderType.Vertex;         break;
                    case "tc": ShaderType = GalShaderType.TessControl;    break;
                    case "te": ShaderType = GalShaderType.TessEvaluation; break;
                    case "g":  ShaderType = GalShaderType.Geometry;       break;
                    case "f":  ShaderType = GalShaderType.Fragment;       break;
                }

                using (FileStream FS = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    Memory Mem = new Memory(FS);

                    string code = Translator.Translate(Mem, 0, ShaderType);

                    Console.WriteLine(code);

                    //GlslProgram Program = Decompiler.Decompile(Mem, 0, ShaderType);

                    //Console.WriteLine(Program.Code);
                }
            }
            else
            {
                Console.WriteLine("Usage: Ryujinx.ShaderTools [v|tc|te|g|f] shader.bin");
            }
        }
    }
}
