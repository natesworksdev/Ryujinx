using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void Ald(EmitterContext context)
        {
            OpCodeAttribute op = (OpCodeAttribute)context.CurrOp;

            Operand[] elems = new Operand[op.Count];

            for (int index = 0; index < op.Count; index++)
            {
                Operand src = Attribute(op.AttributeOffset + index * 4);

                context.Copy(elems[index] = Local(), src);
            }

            for (int index = 0; index < op.Count; index++)
            {
                Register rd = new Register(op.Rd.Index + index, RegisterType.Gpr);

                if (rd.IsRZ)
                {
                    break;
                }

                context.Copy(Register(rd), elems[index]);
            }
        }

        public static void Ast(EmitterContext context)
        {
            OpCodeAttribute op = (OpCodeAttribute)context.CurrOp;

            for (int index = 0; index < op.Count; index++)
            {
                Register rd = new Register(op.Rd.Index + index, RegisterType.Gpr);

                if (rd.IsRZ)
                {
                    break;
                }

                Operand dest = Attribute(op.AttributeOffset + index * 4);

                context.Copy(dest, Register(rd));
            }
        }

        public static void Ipa(EmitterContext context)
        {
            OpCodeIpa op = (OpCodeIpa)context.CurrOp;

            Operand srcA = new Operand(OperandType.Attribute, op.AttributeOffset);

            Operand srcB = GetSrcB(context);

            context.Copy(GetDest(context), srcA);
        }

        public static void Ldc(EmitterContext context)
        {
            OpCodeLdc op = (OpCodeLdc)context.CurrOp;

            if (op.Size > IntegerSize.B64)
            {
                //TODO: Warning.
            }

            bool isSmallInt = op.Size < IntegerSize.B32;

            int count = op.Size == IntegerSize.B64 ? 2 : 1;

            Operand baseOffset = context.Copy(GetSrcA(context));

            for (int index = 0; index < count; index++)
            {
                Register rd = new Register(op.Rd.Index + index, RegisterType.Gpr);

                if (rd.IsRZ)
                {
                    break;
                }

                Operand offset = context.IAdd(baseOffset, Const((op.Offset + index) * 4));

                Operand value = context.LoadConstant(Const(op.Slot), offset);

                if (isSmallInt)
                {
                    Operand shift = context.BitwiseAnd(baseOffset, Const(3));

                    value = context.ShiftRightU32(value, shift);

                    switch (op.Size)
                    {
                        case IntegerSize.U8:  value = ZeroExtendTo32(context, value, 8);  break;
                        case IntegerSize.U16: value = ZeroExtendTo32(context, value, 16); break;
                        case IntegerSize.S8:  value = SignExtendTo32(context, value, 8);  break;
                        case IntegerSize.S16: value = SignExtendTo32(context, value, 16); break;
                    }
                }

                context.Copy(Register(rd), value);
            }
        }

        public static void Out(EmitterContext context)
        {
            OpCode op = context.CurrOp;

            bool emit = op.RawOpCode.Extract(39);
            bool cut  = op.RawOpCode.Extract(40);

            if (!(emit || cut))
            {
                //TODO: Warning.
            }

            if (emit)
            {
                context.EmitVertex();
            }

            if (cut)
            {
                context.EndPrimitive();
            }
        }

        public static void Tex(EmitterContext context)
        {
            OpCodeTex op = (OpCodeTex)context.CurrOp;

            if (op.Rd.IsRZ)
            {
                return;
            }

            int raIndex = op.Ra.Index;
            int rbIndex = op.Rb.Index;

            Operand Ra()
            {
                if (raIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return Register(new Register(raIndex++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (rbIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return Register(new Register(rbIndex++, RegisterType.Gpr));
            }

            Operand arrayIndex = op.IsArray ? Ra() : null;

            List<Operand> sourcesList = new List<Operand>();

            switch (op.Dimensions)
            {
                case TextureDimensions.Texture1D:
                    sourcesList.Add(Ra());
                    break;

                case TextureDimensions.Texture2D:
                    sourcesList.Add(Ra());
                    sourcesList.Add(Ra());
                    break;

                case TextureDimensions.Texture3D:
                case TextureDimensions.TextureCube:
                    sourcesList.Add(Ra());
                    sourcesList.Add(Ra());
                    sourcesList.Add(Ra());
                    break;
            }

            int elemsCount = sourcesList.Count;

            TextureType type = GetTextureType(op.Dimensions);

            if (op.IsArray)
            {
                sourcesList.Add(arrayIndex);

                type |= TextureType.Array;
            }

            bool hasLod = op.LodMode > TextureLodMode.LodZero;

            Operand lodValue = hasLod ? Rb() : ConstF(0);

            Operand packedOffs = op.HasOffset ? Rb() : null;

            if (op.HasDepthCompare)
            {
                sourcesList.Add(Rb());
            }

            if (op.LodMode == TextureLodMode.LodZero  ||
                op.LodMode == TextureLodMode.LodLevel ||
                op.LodMode == TextureLodMode.LodLevelA)
            {
                sourcesList.Add(lodValue);

                type |= TextureType.LodLevel;
            }

            if (op.HasOffset)
            {
                for (int index = 0; index < elemsCount; index++)
                {
                    sourcesList.Add(context.BitfieldExtractS32(packedOffs, Const(index * 4), Const(4)));
                }

                type |= TextureType.Offset;
            }

            if (op.LodMode == TextureLodMode.LodBias ||
                op.LodMode == TextureLodMode.LodBiasA)
            {
                sourcesList.Add(lodValue);

                type |= TextureType.LodBias;
            }

            Operand[] sources = sourcesList.ToArray();

            int rdIndex = op.Rd.Index;

            Operand GetDest()
            {
                if (rdIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return Register(new Register(rdIndex++, RegisterType.Gpr));
            }

            int textureHandle = op.Immediate;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        Instruction.TextureSample,
                        type,
                        textureHandle,
                        compIndex,
                        dest,
                        sources);

                    context.Add(operation);
                }
            }
        }

        public static void Texs(EmitterContext context)
        {
            OpCodeTexs op = (OpCodeTexs)context.CurrOp;

            if (op.Rd0.IsRZ && op.Rd1.IsRZ)
            {
                return;
            }

            List<Operand> sourcesList = new List<Operand>();

            Operand GetSource(int index)
            {
                int regIndex = 0;

                switch (index & 2)
                {
                    case 0: regIndex = op.Ra.Index; break;
                    case 2: regIndex = op.Rb.Index; break;
                }

                if (regIndex != RegisterConsts.RegisterZeroIndex)
                {
                    regIndex += index & 1;
                }

                return context.Copy(Register(new Register(regIndex, RegisterType.Gpr)));
            }

            switch (op.Type)
            {
                case TexsType.Texture1DLodZero:
                    sourcesList.Add(GetSource(0));
                    break;

                case TexsType.Texture2D:
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(2));
                    break;

                case TexsType.Texture2DLodZero:
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DLodLevel:
                case TexsType.Texture2DDepthCompare:
                case TexsType.Texture3D:
                case TexsType.TextureCube:
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    break;

                case TexsType.Texture2DLodZeroDepthCompare:
                case TexsType.Texture3DLodZero:
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DLodLevelDepthCompare:
                case TexsType.TextureCubeLodLevel:
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(GetSource(3));
                    break;

                case TexsType.Texture2DArray:
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(GetSource(0));
                    break;

                case TexsType.Texture2DArrayLodZero:
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DArrayLodZeroDepthCompare:
                    sourcesList.Add(GetSource(1));
                    sourcesList.Add(GetSource(2));
                    sourcesList.Add(GetSource(0));
                    sourcesList.Add(GetSource(3));
                    sourcesList.Add(ConstF(0));
                    break;
            }

            Operand[] sources = sourcesList.ToArray();

            TextureType type = GetTextureType(op.Type);

            int destIncrement = 0;

            Operand GetDest()
            {
                int rdIndex;

                if (op.Rd1.IsRZ)
                {
                    rdIndex = op.Rd0.Index;
                }
                else if (op.Rd0.IsRZ)
                {
                    rdIndex = op.Rd1.Index;
                }
                else
                {
                    rdIndex = (destIncrement >> 1) != 0 ? op.Rd1.Index : op.Rd0.Index;
                }

                rdIndex += destIncrement++ & 1;

                return Register(new Register(rdIndex, RegisterType.Gpr));
            }

            int textureHandle = op.Immediate;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        Instruction.TextureSample,
                        type,
                        textureHandle,
                        compIndex,
                        dest,
                        sources);

                    context.Add(operation);
                }
            }
        }

        private static TextureType GetTextureType(TextureDimensions dimensions)
        {
            switch (dimensions)
            {
                case TextureDimensions.Texture1D:   return TextureType.Texture1D;
                case TextureDimensions.Texture2D:   return TextureType.Texture2D;
                case TextureDimensions.Texture3D:   return TextureType.Texture3D;
                case TextureDimensions.TextureCube: return TextureType.TextureCube;
            }

            throw new ArgumentException($"Invalid texture dimensions \"{dimensions}\".");
        }

        private static TextureType GetTextureType(TexsType type)
        {
            switch (type)
            {
                case TexsType.Texture1DLodZero:
                    return TextureType.Texture1D | TextureType.LodLevel;

                case TexsType.Texture2D:
                    return TextureType.Texture2D;

                case TexsType.Texture2DLodZero:
                case TexsType.Texture2DLodLevel:
                    return TextureType.Texture2D | TextureType.LodLevel;

                case TexsType.Texture2DDepthCompare:
                    return TextureType.Texture2D | TextureType.DepthCompare;

                case TexsType.Texture2DLodLevelDepthCompare:
                case TexsType.Texture2DLodZeroDepthCompare:
                    return TextureType.Texture2D | TextureType.LodLevelDepthCompare;

                case TexsType.Texture2DArray:
                    return TextureType.Texture2D | TextureType.Array;

                case TexsType.Texture2DArrayLodZero:
                    return TextureType.Texture2D | TextureType.Array | TextureType.LodLevel;

                case TexsType.Texture2DArrayLodZeroDepthCompare:
                    return TextureType.Texture2D | TextureType.Array | TextureType.LodLevelDepthCompare;

                case TexsType.Texture3D:
                    return TextureType.Texture3D;

                case TexsType.Texture3DLodZero:
                    return TextureType.Texture3D | TextureType.LodLevel;

                case TexsType.TextureCube:
                    return TextureType.TextureCube;

                case TexsType.TextureCubeLodLevel:
                    return TextureType.TextureCube | TextureType.LodLevel;
            }

            throw new ArgumentException($"Invalid texture type \"{type}\".");
        }
    }
}