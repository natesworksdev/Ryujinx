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
                if (op.Rd.Index + index > RegisterConsts.RegisterZeroIndex)
                {
                    break;
                }

                Register rd = new Register(op.Rd.Index + index, RegisterType.Gpr);

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
            Tex(context, TextureFlags.None);
        }

        public static void Tex_B(EmitterContext context)
        {
            Tex(context, TextureFlags.Bindless);
        }

        public static void Tld(EmitterContext context)
        {
            Tex(context, TextureFlags.IntCoords);
        }

        public static void Tld_B(EmitterContext context)
        {
            Tex(context, TextureFlags.IntCoords | TextureFlags.Bindless);
        }

        public static void Texs(EmitterContext context)
        {
            OpCodeTextureScalar op = (OpCodeTextureScalar)context.CurrOp;

            if (op.Rd0.IsRZ && op.Rd1.IsRZ)
            {
                return;
            }

            List<Operand> sourcesList = new List<Operand>();

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

            TextureType  type;
            TextureFlags flags;

            if (op is OpCodeTexs texsOp)
            {
                type  = GetTextureType (texsOp.Type);
                flags = GetTextureFlags(texsOp.Type);

                if ((type & TextureType.Array) != 0)
                {
                    Operand arrayIndex = Ra();

                    sourcesList.Add(Ra());
                    sourcesList.Add(Rb());

                    sourcesList.Add(arrayIndex);

                    if ((type & TextureType.Shadow) != 0)
                    {
                        sourcesList.Add(Rb());
                    }

                    if ((flags & TextureFlags.LodLevel) != 0)
                    {
                        sourcesList.Add(ConstF(0));
                    }
                }
                else
                {
                    switch (texsOp.Type)
                    {
                        case TexsType.Texture1DLodZero:
                            sourcesList.Add(Ra());
                            break;

                        case TexsType.Texture2D:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            break;

                        case TexsType.Texture2DLodZero:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(ConstF(0));
                            break;

                        case TexsType.Texture2DLodLevel:
                        case TexsType.Texture2DDepthCompare:
                        case TexsType.Texture3D:
                        case TexsType.TextureCube:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            break;

                        case TexsType.Texture2DLodZeroDepthCompare:
                        case TexsType.Texture3DLodZero:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(ConstF(0));
                            break;

                        case TexsType.Texture2DLodLevelDepthCompare:
                        case TexsType.TextureCubeLodLevel:
                            sourcesList.Add(Ra());
                            sourcesList.Add(Ra());
                            sourcesList.Add(Rb());
                            sourcesList.Add(Rb());
                            break;
                    }
                }
            }
            else if (op is OpCodeTlds tldsOp)
            {
                type  = GetTextureType (tldsOp.Type);
                flags = GetTextureFlags(tldsOp.Type) | TextureFlags.IntCoords;

                switch (tldsOp.Type)
                {
                    case TldsType.Texture1DLodZero:
                        sourcesList.Add(Ra());
                        sourcesList.Add(ConstF(0));
                        break;

                    case TldsType.Texture1DLodLevel:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        break;

                    case TldsType.Texture2DLodZero:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        sourcesList.Add(ConstF(0));
                        break;

                    case TldsType.Texture2DLodZeroOffset:
                    case TldsType.Texture2DLodZeroMultisample:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(ConstF(0));
                        sourcesList.Add(Rb());
                        break;

                    case TldsType.Texture2DLodLevel:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        break;

                    case TldsType.Texture3DLodZero:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        sourcesList.Add(ConstF(0));
                        break;

                    case TldsType.Texture2DArrayLodZero:
                        sourcesList.Add(Rb());
                        sourcesList.Add(Rb());
                        sourcesList.Add(Ra());
                        sourcesList.Add(ConstF(0));
                        break;

                    case TldsType.Texture2DLodLevelOffset:
                        sourcesList.Add(Ra());
                        sourcesList.Add(Ra());
                        sourcesList.Add(Rb());
                        sourcesList.Add(Rb());
                        break;
                }
            }
            else if (op is OpCodeTld4s tld4sOp)
            {
                if (!(tld4sOp.HasDepthCompare || tld4sOp.HasOffset))
                {
                    sourcesList.Add(Ra());
                    sourcesList.Add(Rb());
                }
                else
                {
                    sourcesList.Add(Ra());
                    sourcesList.Add(Ra());
                }

                type  = TextureType.Texture2D;
                flags = TextureFlags.Gather;

                if (tld4sOp.HasOffset)
                {
                    sourcesList.Add(Rb());

                    flags |= TextureFlags.Offset;
                }

                if (tld4sOp.HasDepthCompare)
                {
                    sourcesList.Add(Rb());

                    type |= TextureType.Shadow;
                }
            }
            else
            {
                throw new InvalidOperationException($"Invalid opcode type \"{op.GetType().Name}\".");
            }

            Operand[] sources = sourcesList.ToArray();

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

            int handle = op.Immediate;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        Instruction.TextureSample,
                        type,
                        flags,
                        handle,
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
            OpCodeTld4 op = (OpCodeTld4)context.CurrOp;

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

            TextureFlags flags = TextureFlags.Gather;

            int coordsCount = type.GetCoordsCount();

            for (int index = 0; index < coordsCount; index++)
            {
                sourcesList.Add(Ra());
            }

            if (op.IsArray)
            {
                sourcesList.Add(arrayIndex);

                type |= TextureType.Array;
            }

            Operand[] packedOffs = new Operand[2];

            packedOffs[0] = op.Offset != TextureGatherOffset.None    ? Rb() : null;
            packedOffs[1] = op.Offset == TextureGatherOffset.Offsets ? Rb() : null;

            if (op.HasDepthCompare)
            {
                sourcesList.Add(Rb());
            }

            if (op.Offset != TextureGatherOffset.None)
            {
                int offsetTexelsCount = op.Offset == TextureGatherOffset.Offsets ? 4 : 1;

                for (int index = 0; index < coordsCount * offsetTexelsCount; index++)
                {
                    Operand packed = packedOffs[(index >> 2) & 1];

                    sourcesList.Add(context.BitfieldExtractS32(packed, Const((index & 3) * 8), Const(6)));
                }

                flags |= op.Offset == TextureGatherOffset.Offsets
                    ? TextureFlags.Offsets
                    : TextureFlags.Offset;
            }

            sourcesList.Add(Const(op.GatherCompIndex));

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

            int handle = op.Immediate;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        Instruction.TextureSample,
                        type,
                        flags,
                        handle,
                        compIndex,
                        dest,
                        sources);

                    context.Add(operation);
                }
            }
        }

        public static void Txq(EmitterContext context)
        {
            Txq(context, bindless: false);
        }

        public static void Txq_B(EmitterContext context)
        {
            Txq(context, bindless: true);
        }

        private static void Txq(EmitterContext context, bool bindless)
        {
            OpCodeTex op = (OpCodeTex)context.CurrOp;

            if (op.Rd.IsRZ)
            {
                return;
            }

            TextureProperty property = (TextureProperty)op.RawOpCode.Extract(22, 6);

            //TODO: Validate and use property.
            Instruction inst = Instruction.TextureSize;

            TextureType type = TextureType.Texture2D;

            TextureFlags flags = bindless ? TextureFlags.Bindless : TextureFlags.None;

            int raIndex = op.Ra.Index;

            Operand Ra()
            {
                if (raIndex > RegisterConsts.RegisterZeroIndex)
                {
                    return Const(0);
                }

                return context.Copy(Register(raIndex++, RegisterType.Gpr));
            }

            List<Operand> sourcesList = new List<Operand>();

            if (bindless)
            {
                sourcesList.Add(Ra());
            }

            sourcesList.Add(Ra());

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

            int handle = !bindless ? op.Immediate : 0;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        inst,
                        type,
                        flags,
                        handle,
                        compIndex,
                        dest,
                        sources);

                    context.Add(operation);
                }
            }
        }

        private static void Tex(EmitterContext context, TextureFlags flags)
        {
            OpCodeTexture op = (OpCodeTexture)context.CurrOp;

            bool isBindless = (flags & TextureFlags.Bindless)  != 0;
            bool intCoords  = (flags & TextureFlags.IntCoords) != 0;

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

            if (isBindless)
            {
                sourcesList.Add(Rb());
            }

            TextureType type = GetTextureType(op.Dimensions);

            int coordsCount = type.GetCoordsCount();

            for (int index = 0; index < coordsCount; index++)
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

            if (op is OpCodeTex texOp && texOp.HasDepthCompare)
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
                for (int index = 0; index < coordsCount; index++)
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

            if (op is OpCodeTld tldOp && tldOp.IsMultisample)
            {
                sourcesList.Add(Rb());
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

            int handle = !isBindless ? op.Immediate : 0;

            for (int compMask = op.ComponentMask, compIndex = 0; compMask != 0; compMask >>= 1, compIndex++)
            {
                if ((compMask & 1) != 0)
                {
                    Operand dest = GetDest();

                    TextureOperation operation = new TextureOperation(
                        Instruction.TextureSample,
                        type,
                        flags,
                        handle,
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

        private static TextureType GetTextureType(TldsType type)
        {
            switch (type)
            {
                case TldsType.Texture1DLodZero:
                case TldsType.Texture1DLodLevel:
                    return TextureType.Texture1D;

                case TldsType.Texture2DLodZero:
                case TldsType.Texture2DLodZeroOffset:
                case TldsType.Texture2DLodLevel:
                case TldsType.Texture2DLodLevelOffset:
                    return TextureType.Texture2D;

                case TldsType.Texture2DLodZeroMultisample:
                    return TextureType.Texture2D | TextureType.Multisample;

                case TldsType.Texture3DLodZero:
                    return TextureType.Texture3D;

                case TldsType.Texture2DArrayLodZero:
                    return TextureType.Texture2D | TextureType.Array;
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

        private static TextureFlags GetTextureFlags(TldsType type)
        {
            switch (type)
            {
                case TldsType.Texture1DLodZero:
                case TldsType.Texture1DLodLevel:
                case TldsType.Texture2DLodZero:
                case TldsType.Texture2DLodLevel:
                case TldsType.Texture2DLodZeroMultisample:
                case TldsType.Texture3DLodZero:
                case TldsType.Texture2DArrayLodZero:
                    return TextureFlags.LodLevel;

                case TldsType.Texture2DLodZeroOffset:
                case TldsType.Texture2DLodLevelOffset:
                    return TextureFlags.LodLevel | TextureFlags.Offset;
            }

            throw new ArgumentException($"Invalid texture type \"{type}\".");
        }
    }
}