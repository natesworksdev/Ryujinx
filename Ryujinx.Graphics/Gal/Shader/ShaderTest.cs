using System;
using System.Collections.Generic;

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
                System.Console.WriteLine($"{GetOutOperName(Node.Dst)} = {GetInOperName(Node.Src, true)}");
            }

            System.Console.WriteLine("Test code finished!");
        }

        private static string GetOutOperName(ShaderIrOper Oper)
        {
            switch (Oper)
            {
                case ShaderIrOperAbuf Abuf: return GetOAbufName(Abuf);
                case ShaderIrOperReg  Reg:  return GetRegName(Reg);
                
                default: throw new ArgumentException(nameof(Oper));
            }
        }

        private static string GetInOperName(ShaderIrOper Oper, bool Entry = false)
        {
            switch (Oper)
            {
                case ShaderIrOperAbuf Abuf: return GetIAbufName(Abuf);
                case ShaderIrOperCbuf Cbuf: return GetCbufName(Cbuf);
                case ShaderIrOperReg  Reg:  return GetRegName(Reg);
                case ShaderIrOperOp   Op:
                    string Expr;

                    if (InstsExpr.TryGetValue(Op.Inst, out GetInstExpr GetExpr))
                    {
                        Expr = GetExpr(Op);
                    }
                    else
                    {
                        throw new NotImplementedException(Op.Inst.ToString());
                    }

                    if (!(Entry || IsUnary(Op.Inst)))
                    {
                        Expr = $"({Expr})";
                    }

                    return Expr;
                
                default: throw new ArgumentException(nameof(Oper));
            }
        }

        private static bool IsUnary(ShaderIrInst Inst)
        {
            return Inst == ShaderIrInst.Fabs ||
                   Inst == ShaderIrInst.Fneg; 
        }

        private static string GetOAbufName(ShaderIrOperAbuf Abuf)
        {
            return $"a_out[0x{Abuf.Offs:x} + {GetRegName(Abuf.GprIndex)}]";
        }

        private static string GetIAbufName(ShaderIrOperAbuf Abuf)
        {
            return $"a_in[0x{Abuf.Offs:x} + {GetRegName(Abuf.GprIndex)}]";
        }

        private static string GetCbufName(ShaderIrOperCbuf Cbuf)
        {
            return $"c{Cbuf.Index}[{Cbuf.Offs}]";
        }

        private static string GetRegName(ShaderIrOperReg Reg)
        {
            return GetRegName(Reg.GprIndex);
        }

        private static string GetRegName(int GprIndex)
        {
            return GprIndex == 0xff ? "0" : $"r{GprIndex}";
        }

        private delegate string GetInstExpr(ShaderIrOperOp Op);

        private static Dictionary<ShaderIrInst, GetInstExpr> InstsExpr = new
                       Dictionary<ShaderIrInst, GetInstExpr>()
        {
            { ShaderIrInst.Fabs, GetFabsExpr },
            { ShaderIrInst.Fadd, GetFaddExpr },
            { ShaderIrInst.Ffma, GetFfmaExpr },
            { ShaderIrInst.Fmul, GetFmulExpr },
            { ShaderIrInst.Fneg, GetFnegExpr },
        };

        private static string GetFabsExpr(ShaderIrOperOp Op)
        {
            return $"abs({GetInOperName(Op.OperandA)})";
        }

        private static string GetFaddExpr(ShaderIrOperOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} + " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private static string GetFfmaExpr(ShaderIrOperOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} * " +
                   $"{GetInOperName(Op.OperandB)} + " +
                   $"{GetInOperName(Op.OperandC)}";
        }

        private static string GetFmulExpr(ShaderIrOperOp Op)
        {
            return $"{GetInOperName(Op.OperandA)} * " +
                   $"{GetInOperName(Op.OperandB)}";
        }

        private static string GetFnegExpr(ShaderIrOperOp Op)
        {
            return $"-{GetInOperName(Op.OperandA)}";
        }
    }
}