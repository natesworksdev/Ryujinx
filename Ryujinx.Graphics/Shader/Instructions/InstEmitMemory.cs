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

                return context.Copy(Register(raIndex++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (rbIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(rbIndex++, RegisterType.Gpr));
            }

            Operand arrayIndex = op.IsArray ? Ra() : null;

            List<Operand> sourcesList = new List<Operand>();

            TextureType type = GetTextureType(op.Dimensions);

            TextureFlags flags = TextureFlags.None;

            int elemsCount = GetTextureCoordsCount(type);

            for (int index = 0; index < elemsCount; index++)
            {
                sourcesList.Add(Ra());
            }

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

                flags |= TextureFlags.LodLevel;
            }

            if (op.HasOffset)
            {
                for (int index = 0; index < elemsCount; index++)
                {
                    sourcesList.Add(context.BitfieldExtractS32(packedOffs, Const(index * 4), Const(4)));
                }

                flags |= TextureFlags.Offset;
            }

            if (op.LodMode == TextureLodMode.LodBias ||
                op.LodMode == TextureLodMode.LodBiasA)
            {
                sourcesList.Add(lodValue);

                flags |= TextureFlags.LodBias;
            }

            Operand[] sources = sourcesList.ToArray();

            int rdIndex = op.Rd.Index;

            Operand GetDest()
            {
                if (rdIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return Register(rdIndex++, RegisterType.Gpr);
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
                        flags,
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

            Operand Src(int index)
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

                return context.Copy(Register(regIndex, RegisterType.Gpr));
            }

            switch (op.Type)
            {
                case TexsType.Texture1DLodZero:
                    sourcesList.Add(Src(0));
                    break;

                case TexsType.Texture2D:
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(2));
                    break;

                case TexsType.Texture2DLodZero:
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DLodLevel:
                case TexsType.Texture2DDepthCompare:
                case TexsType.Texture3D:
                case TexsType.TextureCube:
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    break;

                case TexsType.Texture2DLodZeroDepthCompare:
                case TexsType.Texture3DLodZero:
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DLodLevelDepthCompare:
                case TexsType.TextureCubeLodLevel:
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(Src(3));
                    break;

                case TexsType.Texture2DArray:
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(Src(0));
                    break;

                case TexsType.Texture2DArrayLodZero:
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(Src(0));
                    sourcesList.Add(ConstF(0));
                    break;

                case TexsType.Texture2DArrayLodZeroDepthCompare:
                    sourcesList.Add(Src(1));
                    sourcesList.Add(Src(2));
                    sourcesList.Add(Src(0));
                    sourcesList.Add(Src(3));
                    sourcesList.Add(ConstF(0));
                    break;
            }

            Operand[] sources = sourcesList.ToArray();

            TextureType  type  = GetTextureType (op.Type);
            TextureFlags flags = GetTextureFlags(op.Type);

            Operand[] rd0 = new Operand[2] { ConstF(0), ConstF(0) };
            Operand[] rd1 = new Operand[2] { ConstF(0), ConstF(0) };

            int destIncrement = 0;

            Operand GetDest()
            {
                int high = destIncrement >> 1;
                int low  = destIncrement &  1;

                destIncrement++;

                if (op.IsFp16)
                {
                    return high != 0
                        ? (rd1[low] = Local())
                        : (rd0[low] = Local());
                }
                else
                {
                    int rdIndex = high != 0 ? op.Rd1.Index : op.Rd0.Index;

                    if (rdIndex < RegisterConsts.RegisterZeroIndex)
                    {
                        rdIndex += low;
                    }

                    return Register(rdIndex, RegisterType.Gpr);
                }
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
                        flags,
                        textureHandle,
                        compIndex,
                        dest,
                        sources);

                    context.Add(operation);
                }
            }

            if (op.IsFp16)
            {
                context.Copy(Register(op.Rd0), context.PackHalf2x16(rd0[0], rd0[1]));
                context.Copy(Register(op.Rd1), context.PackHalf2x16(rd1[0], rd1[1]));
            }
        }

        public static void Tld4(EmitterContext context)
        {
            OpCodeTex op = (OpCodeTex)context.CurrOp;

            if (op.Rd.IsRZ)
            {
                return;
            }

            TextureGatherOffset offset = (TextureGatherOffset)op.RawOpCode.Extract(54, 2);

            int gatherCompIndex = op.RawOpCode.Extract(56, 2);

            int raIndex = op.Ra.Index;
            int rbIndex = op.Rb.Index;

            Operand Ra()
            {
                if (raIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(raIndex++, RegisterType.Gpr));
            }

            Operand Rb()
            {
                if (rbIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(rbIndex++, RegisterType.Gpr));
            }

            Operand arrayIndex = op.IsArray ? Ra() : null;

            List<Operand> sourcesList = new List<Operand>();

            TextureType type = GetTextureType(op.Dimensions);

            TextureFlags flags = TextureFlags.Gather;

            int elemsCount = GetTextureCoordsCount(type);

            for (int index = 0; index < elemsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (op.IsArray)
            {
                sourcesList.Add(arrayIndex);

                type |= TextureType.Array;
            }

            Operand[] packedOffs = new Operand[2];

            packedOffs[0] = offset != TextureGatherOffset.None    ? Rb() : null;
            packedOffs[1] = offset == TextureGatherOffset.Offsets ? Rb() : null;

            if (op.HasDepthCompare)
            {
                sourcesList.Add(Rb());
            }

            if (offset != TextureGatherOffset.None)
            {
                int offsetTexelsCount = offset == TextureGatherOffset.Offsets ? 4 : 1;

                for (int index = 0; index < elemsCount * offsetTexelsCount; index++)
                {
                    Operand packed = packedOffs[(index >> 2) & 1];

                    sourcesList.Add(context.BitfieldExtractS32(packed, Const((index & 3) * 8), Const(6)));
                }

                flags |= offset == TextureGatherOffset.Offsets
                    ? TextureFlags.Offsets
                    : TextureFlags.Offset;
            }

            sourcesList.Add(Const(gatherCompIndex));

            Operand[] sources = sourcesList.ToArray();

            int rdIndex = op.Rd.Index;

            Operand GetDest()
            {
                if (rdIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return Register(rdIndex++, RegisterType.Gpr);
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
                        flags,
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

        private static int GetTextureCoordsCount(TextureType type)
        {
            switch (type & TextureType.Mask)
            {
                case TextureType.Texture1D:   return 1;
                case TextureType.Texture2D:   return 2;
                case TextureType.Texture3D:   return 3;
                case TextureType.TextureCube: return 3;
            }

            throw new ArgumentException($"Invalid texture type \"{type}\".");
        }

        private static TextureType GetTextureType(TexsType type)
        {
            switch (type)
            {
                case TexsType.Texture1DLodZero:
                    return TextureType.Texture1D;

                case TexsType.Texture2D:
                case TexsType.Texture2DLodZero:
                case TexsType.Texture2DLodLevel:
                    return TextureType.Texture2D;

                case TexsType.Texture2DDepthCompare:
                case TexsType.Texture2DLodLevelDepthCompare:
                case TexsType.Texture2DLodZeroDepthCompare:
                    return TextureType.Texture2D | TextureType.Shadow;

                case TexsType.Texture2DArray:
                case TexsType.Texture2DArrayLodZero:
                    return TextureType.Texture2D | TextureType.Array;

                case TexsType.Texture2DArrayLodZeroDepthCompare:
                    return TextureType.Texture2D | TextureType.Array | TextureType.Shadow;

                case TexsType.Texture3D:
                case TexsType.Texture3DLodZero:
                    return TextureType.Texture3D;

                case TexsType.TextureCube:
                case TexsType.TextureCubeLodLevel:
                    return TextureType.TextureCube;
            }

            throw new ArgumentException($"Invalid texture type \"{type}\".");
        }

        private static TextureFlags GetTextureFlags(TexsType type)
        {
            switch (type)
            {
                case TexsType.Texture1DLodZero:
                case TexsType.Texture2DLodZero:
                case TexsType.Texture2DLodLevel:
                case TexsType.Texture2DLodLevelDepthCompare:
                case TexsType.Texture2DLodZeroDepthCompare:
                case TexsType.Texture2DArrayLodZero:
                case TexsType.Texture2DArrayLodZeroDepthCompare:
                case TexsType.Texture3DLodZero:
                case TexsType.TextureCubeLodLevel:
                    return TextureFlags.LodLevel;

                case TexsType.Texture2D:
                case TexsType.Texture2DDepthCompare:
                case TexsType.Texture2DArray:
                case TexsType.Texture3D:
                case TexsType.TextureCube:
                    return TextureFlags.None;
            }

            throw new ArgumentException($"Invalid texture type \"{type}\".");
        }
    }
}