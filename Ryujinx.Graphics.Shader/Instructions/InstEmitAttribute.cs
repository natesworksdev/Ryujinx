using Ryujinx.Graphics.Shader.Decoders;
using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using Ryujinx.Graphics.Shader.Translation;

using static Ryujinx.Graphics.Shader.Instructions.InstEmitHelper;
using static Ryujinx.Graphics.Shader.IntermediateRepresentation.OperandHelper;

namespace Ryujinx.Graphics.Shader.Instructions
{
    static partial class InstEmit
    {
        public static void Al2p(EmitterContext context)
        {
            InstAl2p op = context.GetOp<InstAl2p>();

            context.Copy(GetDest(op.Dest), context.IAdd(GetSrcReg(context, op.SrcA), Const(op.Imm11)));
        }

        public static void Ald(EmitterContext context)
        {
            InstAld op = context.GetOp<InstAld>();

            Operand primVertex = context.Copy(GetSrcReg(context, op.SrcB));

            for (int index = 0; index < (int)op.AlSize + 1; index++)
            {
                Register rd = new Register(op.Dest + index, RegisterType.Gpr);

                if (rd.IsRZ)
                {
                    break;
                }

                if (op.Phys)
                {
                    Operand userAttrOffset = context.ISubtract(GetSrcReg(context, op.SrcA), Const(AttributeConsts.UserAttributeBase));
                    Operand userAttrIndex = context.ShiftRightU32(userAttrOffset, Const(2));

                    context.Copy(Register(rd), context.LoadAttribute(Const(AttributeConsts.UserAttributeBase), userAttrIndex, primVertex));

                    context.Config.SetUsedFeature(FeatureFlags.IaIndexing);
                }
                else if (op.SrcB == RegisterConsts.RegisterZeroIndex || op.P)
                {
                    int offset = FixedFuncToUserAttribute(context.Config, op.Imm11 + index * 4, op.O);

                    context.FlagAttributeRead(offset);

                    if (op.O)
                    {
                        offset |= AttributeConsts.LoadOutputMask;
                    }

                    Operand src = op.P ? AttributePerPatch(offset) : Attribute(offset);

                    context.Copy(Register(rd), src);
                }
                else
                {
                    int offset = FixedFuncToUserAttribute(context.Config, op.Imm11 + index * 4, op.O);

                    context.FlagAttributeRead(offset);

                    if (op.O)
                    {
                        offset |= AttributeConsts.LoadOutputMask;
                    }

                    Operand src = Const(offset);

                    context.Copy(Register(rd), context.LoadAttribute(src, Const(0), primVertex));
                }
            }
        }

        public static void Ast(EmitterContext context)
        {
            InstAst op = context.GetOp<InstAst>();

            for (int index = 0; index < (int)op.AlSize + 1; index++)
            {
                if (op.SrcB + index > RegisterConsts.RegisterZeroIndex)
                {
                    break;
                }

                Register rd = new Register(op.SrcB + index, RegisterType.Gpr);

                if (op.Phys)
                {
                    Operand userAttrOffset = context.ISubtract(GetSrcReg(context, op.SrcA), Const(AttributeConsts.UserAttributeBase));
                    Operand userAttrIndex = context.ShiftRightU32(userAttrOffset, Const(2));

                    context.StoreAttribute(Const(AttributeConsts.UserAttributeBase), userAttrIndex, Register(rd));

                    context.Config.SetUsedFeature(FeatureFlags.OaIndexing);
                }
                else
                {
                    // TODO: Support indirect stores using Ra.

                    int offset = op.Imm11 + index * 4;

                    if (!context.Config.IsUsedOutputAttribute(offset))
                    {
                        return;
                    }

                    offset = FixedFuncToUserAttribute(context.Config, offset, isOutput: true);

                    context.FlagAttributeWritten(offset);

                    Operand dest = op.P ? AttributePerPatch(offset) : Attribute(offset);

                    context.Copy(dest, Register(rd));
                }
            }
        }

        public static void Ipa(EmitterContext context)
        {
            InstIpa op = context.GetOp<InstIpa>();

            context.FlagAttributeRead(op.Imm10);

            Operand res;

            if (op.Idx)
            {
                Operand userAttrOffset = context.ISubtract(GetSrcReg(context, op.SrcA), Const(AttributeConsts.UserAttributeBase));
                Operand userAttrIndex = context.ShiftRightU32(userAttrOffset, Const(2));

                res = context.LoadAttribute(Const(AttributeConsts.UserAttributeBase), userAttrIndex, Const(0));
                res = context.FPMultiply(res, Attribute(AttributeConsts.PositionW));

                context.Config.SetUsedFeature(FeatureFlags.IaIndexing);
            }
            else
            {
                res = FixedFuncToUserAttributeIpa(context, op.Imm10);

                if (op.Imm10 >= AttributeConsts.UserAttributeBase && op.Imm10 < AttributeConsts.UserAttributeEnd)
                {
                    int index = (op.Imm10 - AttributeConsts.UserAttributeBase) >> 4;

                    if (context.Config.ImapTypes[index].GetFirstUsedType() == PixelImap.Perspective)
                    {
                        res = context.FPMultiply(res, Attribute(AttributeConsts.PositionW));
                    }
                }
            }

            if (op.IpaOp == IpaOp.Multiply)
            {
                Operand srcB = GetSrcReg(context, op.SrcB);

                res = context.FPMultiply(res, srcB);
            }

            res = context.FPSaturate(res, op.Sat);

            context.Copy(GetDest(op.Dest), res);
        }

        public static void Isberd(EmitterContext context)
        {
            InstIsberd op = context.GetOp<InstIsberd>();

            // This instruction performs a load from ISBE memory,
            // however it seems to be only used to get some vertex
            // input data, so we instead propagate the offset so that
            // it can be used on the attribute load.
            context.Copy(GetDest(op.Dest), GetSrcReg(context, op.SrcA));
        }

        public static void OutR(EmitterContext context)
        {
            InstOutR op = context.GetOp<InstOutR>();

            EmitOut(context, op.OutType.HasFlag(OutType.Emit), op.OutType.HasFlag(OutType.Cut));
        }

        public static void OutI(EmitterContext context)
        {
            InstOutI op = context.GetOp<InstOutI>();

            EmitOut(context, op.OutType.HasFlag(OutType.Emit), op.OutType.HasFlag(OutType.Cut));
        }

        public static void OutC(EmitterContext context)
        {
            InstOutC op = context.GetOp<InstOutC>();

            EmitOut(context, op.OutType.HasFlag(OutType.Emit), op.OutType.HasFlag(OutType.Cut));
        }

        private static void EmitOut(EmitterContext context, bool emit, bool cut)
        {
            if (!(emit || cut))
            {
                context.Config.GpuAccessor.Log("Invalid OUT encoding.");
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

        private static Operand FixedFuncToUserAttributeIpa(EmitterContext context, int attr)
        {
            if (attr >= AttributeConsts.FrontColorDiffuseR && attr < AttributeConsts.BackColorDiffuseR)
            {
                int index = (attr - AttributeConsts.FrontColorDiffuseR) >> 4;
                int userAttrIndex = context.Config.GetFreeUserAttribute(isOutput: false, index);
                Operand frontAttr = Attribute(AttributeConsts.UserAttributeBase + userAttrIndex * 16 + (attr & 0xf));
                Operand backAttr = Attribute(AttributeConsts.UserAttributeBase + (userAttrIndex + 2) * 16 + (attr & 0xf));

                context.Config.SetInputUserAttributeFixedFunc(userAttrIndex);
                context.Config.SetInputUserAttributeFixedFunc(userAttrIndex + 2);

                return context.ConditionalSelect(Attribute(AttributeConsts.FrontFacing), frontAttr, backAttr);
            }
            else if (attr >= AttributeConsts.BackColorDiffuseR && attr < AttributeConsts.ClipDistance0)
            {
                return ConstF(((attr >> 2) & 3) == 3 ? 1f : 0f);
            }
            else if (attr >= AttributeConsts.TexCoordBase && attr < AttributeConsts.TexCoordEnd)
            {
                attr = FixedFuncToUserAttribute(context.Config, attr, AttributeConsts.TexCoordBase, 4, isOutput: false);
            }

            return Attribute(attr);
        }

        private static int FixedFuncToUserAttribute(ShaderConfig config, int attr, bool isOutput)
        {
            if (attr >= AttributeConsts.FrontColorDiffuseR && attr < AttributeConsts.ClipDistance0)
            {
                attr = FixedFuncToUserAttribute(config, attr, AttributeConsts.FrontColorDiffuseR, 0, isOutput);
            }
            else if (attr >= AttributeConsts.TexCoordBase && attr < AttributeConsts.TexCoordEnd)
            {
                attr = FixedFuncToUserAttribute(config, attr, AttributeConsts.TexCoordBase, 4, isOutput);
            }

            return attr;
        }

        private static int FixedFuncToUserAttribute(ShaderConfig config, int attr, int baseAttr, int baseIndex, bool isOutput)
        {
            int index = (attr - baseAttr) >> 4;
            int userAttrIndex = config.GetFreeUserAttribute(isOutput, index);

            if ((uint)userAttrIndex < Constants.MaxAttributes)
            {
                userAttrIndex += baseIndex;
                attr = AttributeConsts.UserAttributeBase + userAttrIndex * 16 + (attr & 0xf);

                if (isOutput)
                {
                    config.SetOutputUserAttributeFixedFunc(userAttrIndex);
                }
                else
                {
                    config.SetInputUserAttributeFixedFunc(userAttrIndex);
                }
            }

            return attr;
        }
    }
}