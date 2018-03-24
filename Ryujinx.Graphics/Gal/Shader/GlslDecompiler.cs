using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Gal.Shader
{
    class GlslDecompiler
    {
        private delegate string GetInstExpr(ShaderIrOperOp Op);

        private Dictionary<ShaderIrInst, GetInstExpr> InstsExpr;

        private class Attrib
        {
            public string Name;
            public int    Elems;

            public Attrib(string Name, int Elems)
            {
                this.Name  = Name;
                this.Elems = Elems;
            }
        }

        private static string[] ElemTypes = new string[] { "float", "vec2", "vec3", "vec4" };

        private SortedDictionary<int, Attrib> InputAttributes;
        private SortedDictionary<int, Attrib> OutputAttributes;

        private HashSet<int> UsedCbufs;

        private const int AttrStartIndex = 8;

        private const string InputAttrPrefix  = "in_attr";
        private const string OutputAttrPrefix = "out_attr";

        private const string CbufBuffPrefix = "c";
        private const string CbufDataName   = "buf";

        private const string GprName = "gpr";

        private const string IdentationStr = "\t";

        private int GprsCount;

        private StringBuilder BodySB;

        public GlslDecompiler()
        {
            InstsExpr = new Dictionary<ShaderIrInst, GetInstExpr>()
            {
                { ShaderIrInst.Fabs, GetFabsExpr },
                { ShaderIrInst.Fadd, GetFaddExpr },
                { ShaderIrInst.Ffma, GetFfmaExpr },
                { ShaderIrInst.Fmul, GetFmulExpr },
                { ShaderIrInst.Fneg, GetFnegExpr }
            };
        }

        public string Decompile(int[] Code)
        {
            InputAttributes  = new SortedDictionary<int, Attrib>();
            OutputAttributes = new SortedDictionary<int, Attrib>();

            UsedCbufs = new HashSet<int>();

            BodySB = new StringBuilder();

            //FIXME: Only valid for vertex shaders.
            OutputAttributes.Add(7, new Attrib("gl_Position", 4));

            ShaderIrBlock Block = ShaderDecoder.DecodeBasicBlock(Code, 0);

            ShaderIrNode[] Nodes = Block.GetNodes();

            PrintBlockScope(Nodes, "void main()", 1);

            StringBuilder SB = new StringBuilder();

            PrintDeclUBOs(SB);
            PrintDeclInAttributes(SB);
            PrintDeclOutAttributes(SB);

            if (GprsCount > 0)
            {
                SB.AppendLine($"float {GprName}[{GprsCount}];");
                SB.AppendLine(string.Empty);
            }

            SB.Append(BodySB.ToString());

            BodySB.Clear();

            return SB.ToString();
        }

        private void PrintDeclUBOs(StringBuilder SB)
        {
            foreach (int Cbuf in UsedCbufs)
            {
                SB.AppendLine($"layout(std430, binding = {Cbuf}) buffer {CbufBuffPrefix}{Cbuf} {{");
                SB.AppendLine($"{IdentationStr}float[] {CbufDataName};");
                SB.AppendLine("};");
                SB.AppendLine(string.Empty);
            }
        }

        private void PrintDeclInAttributes(StringBuilder SB)
        {
            foreach (KeyValuePair<int, Attrib> KV in InputAttributes)
            {
                int Index = KV.Key - AttrStartIndex;

                if (Index >= 0)
                {
                    string Type = ElemTypes[KV.Value.Elems];

                    SB.AppendLine($"layout(location = {Index}) in {Type} {KV.Value.Name};");
                }
            }

            SB.AppendLine(string.Empty);
        }

        private void PrintDeclOutAttributes(StringBuilder SB)
        {
            foreach (KeyValuePair<int, Attrib> KV in OutputAttributes)
            {
                int Index = KV.Key - AttrStartIndex;

                if (Index >= 0)
                {
                    string Type = ElemTypes[KV.Value.Elems];

                    SB.AppendLine($"layout(location = {Index}) out {Type} {KV.Value.Name};");
                }
            }

            SB.AppendLine(string.Empty);
        }

        private void PrintBlockScope(ShaderIrNode[] Nodes, string ScopeName, int IdentationLevel)
        {
            string Identation = string.Empty;

            for (int Index = 0; Index < IdentationLevel - 1; Index++)
            {
                Identation += IdentationStr;
            }

            if (ScopeName != string.Empty)
            {
                ScopeName += " ";
            }

            BodySB.AppendLine(Identation + ScopeName + "{");

            string LastLine = Identation + "}";

            if (IdentationLevel > 0)
            {
                Identation += IdentationStr;
            }

            foreach (ShaderIrNode Node in Nodes)
            {
                if (Node.Dst is ShaderIrOperReg Reg && Reg.GprIndex == ShaderIrOperReg.ZRIndex)
                {
                    continue;
                }

                BodySB.AppendLine(Identation +
                    $"{GetOOperName(Node.Dst)} = " + 
                    $"{GetIOperName(Node.Src, true)};");
            }

            BodySB.AppendLine(LastLine);
        }

        private string GetOOperName(ShaderIrOper Oper)
        {
            if (Oper is ShaderIrOperAbuf Abuf)
            {
                return GetOAbufName(Abuf);
            }
            else if (Oper is ShaderIrOperReg Reg)
            {
                return GetRegName(Reg);
            }

            throw new ArgumentException(nameof(Oper));
        }

        private string GetIOperName(ShaderIrOper Oper, bool Entry = false)
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

        private bool IsUnary(ShaderIrInst Inst)
        {
            return Inst == ShaderIrInst.Fabs ||
                   Inst == ShaderIrInst.Fneg; 
        }

        private string GetOAbufName(ShaderIrOperAbuf Abuf)
        {
            int AttrIndex = Abuf.Offs >> 4;

            int Elem = (Abuf.Offs >> 2) & 3;

            if (!OutputAttributes.TryGetValue(AttrIndex, out Attrib Attr))
            {
                Attr = new Attrib(OutputAttrPrefix + (AttrIndex - AttrStartIndex), Elem);

                OutputAttributes.Add(AttrIndex, Attr);
            }

            if (Attr.Elems < Elem)
            {
                Attr.Elems = Elem;
            }

            return $"{Attr.Name}.{GetAttrSwizzle(Elem)}";
        }

        private string GetIAbufName(ShaderIrOperAbuf Abuf)
        {
            int AttrIndex = Abuf.Offs >> 4;

            int Elem = (Abuf.Offs >> 2) & 3;

            if (!InputAttributes.TryGetValue(AttrIndex, out Attrib Attr))
            {
                Attr = new Attrib(InputAttrPrefix + (AttrIndex - AttrStartIndex), Elem);

                InputAttributes.Add(AttrIndex, Attr);
            }

            if (Attr.Elems < Elem)
            {
                Attr.Elems = Elem;
            }

            return $"{Attr.Name}.{GetAttrSwizzle(Elem)}";
        }

        private string GetAttrSwizzle(int Elem)
        {
            return "xyzw".Substring(Elem, 1);
        }

        private string GetCbufName(ShaderIrOperCbuf Cbuf)
        {
            UsedCbufs.Add(Cbuf.Index);

            return $"{CbufBuffPrefix}{Cbuf.Index}.{CbufDataName}[{Cbuf.Offs}]";
        }

        private string GetRegName(ShaderIrOperReg Reg)
        {
            if (GprsCount < Reg.GprIndex + 1)
            {
                GprsCount = Reg.GprIndex + 1;
            }

            return GetRegName(Reg.GprIndex);
        }

        private string GetRegName(int GprIndex)
        {
            return GprIndex == ShaderIrOperReg.ZRIndex ? "0" : $"{GprName}[{GprIndex}]";
        }

        private string GetFabsExpr(ShaderIrOperOp Op)
        {
            return $"abs({GetIOperName(Op.OperandA)})";
        }

        private string GetFaddExpr(ShaderIrOperOp Op)
        {
            return $"{GetIOperName(Op.OperandA)} + " +
                   $"{GetIOperName(Op.OperandB)}";
        }

        private string GetFfmaExpr(ShaderIrOperOp Op)
        {
            return $"{GetIOperName(Op.OperandA)} * " +
                   $"{GetIOperName(Op.OperandB)} + " +
                   $"{GetIOperName(Op.OperandC)}";
        }

        private  string GetFmulExpr(ShaderIrOperOp Op)
        {
            return $"{GetIOperName(Op.OperandA)} * " +
                   $"{GetIOperName(Op.OperandB)}";
        }

        private string GetFnegExpr(ShaderIrOperOp Op)
        {
            return $"-{GetIOperName(Op.OperandA)}";
        }
    }
}