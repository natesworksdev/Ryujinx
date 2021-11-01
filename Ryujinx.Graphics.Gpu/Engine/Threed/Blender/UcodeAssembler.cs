using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Engine.Threed.Blender
{
    enum Instruction
    {
        Mmadd = 0,
        Mmsub = 1,
        Min = 2,
        Max = 3,
        Rcp = 4,
        Add = 5,
        Sub = 6
    }

    enum CC
    {
        F = 0,
        T = 1,
        EQ = 2,
        NE = 3,
        LT = 4,
        LE = 5,
        GT = 6,
        GE = 7
    }

    enum OpBD
    {
        ConstantZero = 0x0,
        ConstantOne = 0x1,
        SrcRGB = 0x2,
        SrcAAA = 0x3,
        OneMinusSrcAAA = 0x4,
        DstRGB = 0x5,
        DstAAA = 0x6,
        OneMinusDstAAA = 0x7,
        Temp0 = 0x9,
        Temp1 = 0xa,
        Temp2 = 0xb,
        PBR = 0xc,
        ConstantRGB = 0xd
    }

    enum OpAC
    {
        SrcRGB = 0,
        DstRGB = 1,
        SrcAAA = 2,
        DstAAA = 3,
        Temp0 = 4,
        Temp1 = 5,
        Temp2 = 6,
        PBR = 7
    }

    enum OpDst
    {
        Temp0 = 0,
        Temp1 = 1,
        Temp2 = 2,
        PBR = 3
    }

    enum Swizzle
    {
        RGB = 0,
        GBR = 1,
        RRR = 2,
        GGG = 3,
        BBB = 4,
        RToA = 5
    }

    enum WriteMask
    {
        RGB = 0,
        R = 1,
        G = 2,
        B = 3
    }

    struct RgbFloat
    {
        public float R { get; }
        public float G { get; }
        public float B { get; }

        public RgbFloat(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }
    }

    struct Dest
    {
        public static Dest Temp0 => new Dest(OpDst.Temp0, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest Temp1 => new Dest(OpDst.Temp1, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest Temp2 => new Dest(OpDst.Temp2, Swizzle.RGB, WriteMask.RGB, false);
        public static Dest PBR => new Dest(OpDst.PBR, Swizzle.RGB, WriteMask.RGB, false);

        public Dest GBR => new Dest(Dst, Swizzle.GBR, WriteMask, WriteCC);
        public Dest RRR => new Dest(Dst, Swizzle.RRR, WriteMask, WriteCC);
        public Dest GGG => new Dest(Dst, Swizzle.GGG, WriteMask, WriteCC);
        public Dest BBB => new Dest(Dst, Swizzle.BBB, WriteMask, WriteCC);
        public Dest RToA => new Dest(Dst, Swizzle.RToA, WriteMask, WriteCC);

        public Dest R => new Dest(Dst, Swizzle, WriteMask.R, WriteCC);
        public Dest G => new Dest(Dst, Swizzle, WriteMask.G, WriteCC);
        public Dest B => new Dest(Dst, Swizzle, WriteMask.B, WriteCC);

        public Dest CC => new Dest(Dst, Swizzle, WriteMask, true);

        public OpDst Dst { get; }
        public Swizzle Swizzle { get; }
        public WriteMask WriteMask { get; }
        public bool WriteCC { get; }

        public Dest(OpDst dst, Swizzle swizzle, WriteMask writeMask, bool writeCC)
        {
            Dst = dst;
            Swizzle = swizzle;
            WriteMask = writeMask;
            WriteCC = writeCC;
        }
    }

    struct UcodeOp
    {
        public readonly uint Word;

        public UcodeOp(CC cc, Instruction inst, int constIndex, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Word = (uint)cc |
                ((uint)inst << 3) |
                ((uint)constIndex << 6) |
                ((uint)srcA << 9) |
                ((uint)srcB << 12) |
                ((uint)srcC << 16) |
                ((uint)srcD << 19) |
                ((uint)dest.Swizzle << 23) |
                ((uint)dest.WriteMask << 26) |
                ((uint)dest.Dst << 28) |
                (dest.WriteCC ? (1u << 31) : 0);
        }
    }

    struct UcodeAssembler
    {
        private List<uint> _code;
        private RgbFloat[] _constants;
        private int _constantIndex;

        public void Mul(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Madd(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, srcC, OpBD.ConstantOne);
        }

        public void Mmadd(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Assemble(cc, Instruction.Mmadd, dest, srcA, srcB, srcC, srcD);
        }

        public void Mmsub(CC cc, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            Assemble(cc, Instruction.Mmsub, dest, srcA, srcB, srcC, srcD);
        }

        public void Min(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Min, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Max(CC cc, Dest dest, OpAC srcA, OpBD srcB)
        {
            Assemble(cc, Instruction.Max, dest, srcA, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Rcp(CC cc, Dest dest, OpAC srcA)
        {
            Assemble(cc, Instruction.Rcp, dest, srcA, OpBD.ConstantZero, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Mov(CC cc, Dest dest, OpBD srcB)
        {
            Assemble(cc, Instruction.Add, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, OpBD.ConstantZero);
        }

        public void Add(CC cc, Dest dest, OpBD srcB, OpBD srcD)
        {
            Assemble(cc, Instruction.Add, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, srcD);
        }

        public void Sub(CC cc, Dest dest, OpBD srcB, OpBD srcD)
        {
            Assemble(cc, Instruction.Sub, dest, OpAC.SrcRGB, srcB, OpAC.SrcRGB, srcD);
        }

        private void Assemble(CC cc, Instruction inst, Dest dest, OpAC srcA, OpBD srcB, OpAC srcC, OpBD srcD)
        {
            (_code ??= new List<uint>()).Add(new UcodeOp(cc, inst, _constantIndex, dest, srcA, srcB, srcC, srcD).Word);
        }

        public void SetConstant(int index, float r, float g, float b)
        {
            if (_constants == null)
            {
                _constants = new RgbFloat[index + 1];
            }
            else if (_constants.Length <= index)
            {
                Array.Resize(ref _constants, index + 1);
            }

            _constants[index] = new RgbFloat(r, g, b);
            _constantIndex = index;
        }

        public uint[] GetCode()
        {
            return _code?.ToArray();
        }

        public RgbFloat[] GetConstants()
        {
            return _constants;
        }
    }
}