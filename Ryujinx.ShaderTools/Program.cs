using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.Shader;
using System;
using System.IO;

namespace Ryujinx.ShaderTools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                GlslDecompiler Decompiler = new GlslDecompiler();

                GalShaderType ShaderType = GalShaderType.Vertex;

                switch (args[0].ToLower())
                {
                    case "v":  ShaderType = GalShaderType.Vertex;         break;
                    case "tc": ShaderType = GalShaderType.TessControl;    break;
                    case "te": ShaderType = GalShaderType.TessEvaluation; break;
                    case "g":  ShaderType = GalShaderType.Geometry;       break;
                    case "f":  ShaderType = GalShaderType.Fragment;       break;
                }


                byte[] Binary = File.ReadAllBytes(args[1]);

                GlslProgram Program = Decompiler.Decompile(Binary, ShaderType);

                Console.WriteLine(Program.Code);
            }
            else
            {
                Console.WriteLine("Usage: Ryujinx.ShaderTools [v|tc|te|g|f] shader.bin");
            }
        }
    }
}
