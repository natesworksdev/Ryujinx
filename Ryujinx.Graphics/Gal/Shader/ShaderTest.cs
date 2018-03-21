namespace Ryujinx.Graphics.Gal.Shader
{
    public static class ShaderTest
    {
        public static void Test()
        {
            System.Console.WriteLine("Starting test code...");

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

            ShaderIrBlock Block = ShaderDecoder.DecodeBasicBlock(Code, 0);

            ShaderIrNode[] Nodes = Block.GetNodes();

            foreach (ShaderIrNode Node in Nodes)
            {
                System.Console.Write(Node.Inst);

                if (Node is ShaderIrNodeLdr LdrNode)
                {
                    System.Console.Write($" r{LdrNode.GprIndex}");
                }
                else if (Node is ShaderIrNodeStr StrNode)
                {
                    System.Console.Write($" r{StrNode.GprIndex}");
                }
                else if (Node is ShaderIrNodeLdb LdbNode)
                {
                    System.Console.Write($" c{LdbNode.Cbuf}[0x{LdbNode.Offs.ToString("x")}]");
                }

                System.Console.WriteLine(string.Empty);
            }

            System.Console.WriteLine("Test code finished!");
        }
    }
}