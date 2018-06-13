using Ryujinx.Graphics.Gal;
using Ryujinx.Graphics.Gal.Shader;
using System;
using System.IO;
using System.Text;

namespace Ryujinx.ShaderTools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 4)
            {
                GalShaderType ShaderType = GalShaderType.Vertex;

                switch (args[1].ToLower())
                {
                    case "v":  ShaderType = GalShaderType.Vertex;         break;
                    case "tc": ShaderType = GalShaderType.TessControl;    break;
                    case "te": ShaderType = GalShaderType.TessEvaluation; break;
                    case "g":  ShaderType = GalShaderType.Geometry;       break;
                    case "f":  ShaderType = GalShaderType.Fragment;       break;
                }

                using (FileStream Output = new FileStream(args[3], FileMode.Create))
                using (FileStream FS = new FileStream(args[2], FileMode.Open, FileAccess.Read))
                {
                    Memory Mem = new Memory(FS);

                    switch (args[0].ToLower())
                    {
                        case "glsl":
                            GlslDecompiler GlslDecompiler = new GlslDecompiler();

                            GlslProgram Program = GlslDecompiler.Decompile(Mem, 0, ShaderType);

                            Output.Write(System.Text.Encoding.UTF8.GetBytes(Program.Code));

                            break;

                        case "spirv":
                            SpirvDecompiler SpirvDecompiler = new SpirvDecompiler();

                            Output.Write(SpirvDecompiler.Decompile(Mem, 0, ShaderType));

                            break;
                    }
                }
            }
            else
            {
                Console.WriteLine("Usage: Ryujinx.ShaderTools [spirv|glsl] [v|tc|te|g|f] shader.bin output.bin");
            }
        }
    }
}
