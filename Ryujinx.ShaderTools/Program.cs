using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.Shader;
using System;
using System.IO;

namespace Ryujinx.ShaderTools
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                GlslDecompiler decompiler = new GlslDecompiler();

                GalShaderType shaderType = GalShaderType.Vertex;

                switch (args[0].ToLower())
                {
                    case "v":  shaderType = GalShaderType.Vertex;         break;
                    case "tc": shaderType = GalShaderType.TessControl;    break;
                    case "te": shaderType = GalShaderType.TessEvaluation; break;
                    case "g":  shaderType = GalShaderType.Geometry;       break;
                    case "f":  shaderType = GalShaderType.Fragment;       break;
                }

                using (FileStream fs = new FileStream(args[1], FileMode.Open, FileAccess.Read))
                {
                    Memory mem = new Memory(fs);

                    GlslProgram program = decompiler.Decompile(mem, 0, shaderType);

                    Console.WriteLine(program.Code);
                }
            }
            else
            {
                Console.WriteLine("Usage: Ryujinx.ShaderTools [v|tc|te|g|f] shader.bin");
            }
        }
    }
}
