using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.Shader
{
    public static class ShaderTest
    {
        public static void Test()
        {
            System.Collections.Generic.List<int> CodeList = new System.Collections.Generic.List<int>();

            using (System.IO.FileStream FS = new System.IO.FileStream("D:\\puyo_vsh.bin", System.IO.FileMode.Open))
            {
                System.IO.BinaryReader Reader = new System.IO.BinaryReader(FS);

                while (FS.Position + 8 <= FS.Length)
                {
                    CodeList.Add(Reader.ReadInt32());
                }
            }

            int[] Code = CodeList.ToArray();

            GlslDecompiler Decompiler = new GlslDecompiler();

            System.Console.WriteLine(Decompiler.Decompile(Code));

            System.Console.WriteLine("Done!");
        }
    }
}