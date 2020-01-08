#define SimdMov32

using ARMeilleure.State;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdMov32")]
    public sealed class CpuTestSimdMov32 : CpuTest32
    {
#if SimdMov32
        private const int RndCntImm = 10;

        [Test, Combinatorial, Description("VMOV.I<size> <Dd/Qd>, #<imm>")]
        public void Movi_V([Range(0u, 10u)] uint variant,
        [Values(0u, 1u, 2u, 3u)] uint vd,
        [Values(0x0u)] [Random(1u, 0xffu, RndCntImm)] uint imm,
        [Values] bool q)
        {
            uint[] variants =
            {
                //I32
                0b0000_0,
                0b0010_0,
                0b0100_0,
                0b0110_0,

                //I16
                0b1000_0,
                0b1010_0,

                //dt
                0b1100_0,
                0b1101_0,
                0b1110_0,
                0b1111_0,

                0b1110_1
            };


            uint opcode = 0xf2800010; // vmov.i32 d0, #0
            uint cmodeOp = variants[variant];

            if (q) vd &= 0x1e;

            opcode |= ((cmodeOp & 1) << 5) | ((cmodeOp & 0x1e) << 7);
            opcode |= ((q ? 1u : 0u) << 6);
            opcode |= (imm & 0xf) | ((imm & 0x70) << 12) | ((imm & 0x80) << 16);

            opcode |= ((vd & 0x10) << 18);
            opcode |= ((vd & 0xf) << 12);

            SingleOpcode(opcode); //correct

            CompareAgainstUnicorn();
        }

        [Test, Combinatorial, Description("VMOV.F<size> <Sd>, #<imm>")]
        public void Movi_S([Range(2u, 3u)] uint size, //fp16 is not supported for now
        [Values(0u, 1u, 2u, 3u)] uint vd,
        [Values(0x0u)] [Random(0u, 0xffu, RndCntImm)] uint imm)
        {
            uint opcode = 0xeeb00800; // invalid
            opcode |= (size & 3) << 8;
            opcode |= (imm & 0xf) | ((imm & 0xf0) << 12);

            if (size == 2)
            {
                opcode |= ((vd & 0x1) << 22);
                opcode |= ((vd & 0x1e) << 11);
            }
            else
            {
                opcode |= ((vd & 0x10) << 18);
                opcode |= ((vd & 0xf) << 12);
            }

            SingleOpcode(opcode); //correct

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VMOV <Rd>, <Sd>")]
        public void Mov_GP([Values(0u, 1u, 2u, 3u)] uint vn,
                           [Values(0u, 1u, 2u, 3u)] uint rt,
                           [Random(RndCntImm)] uint valueRn,
                           [Random(RndCntImm)] ulong valueVn1,
                           [Random(RndCntImm)] ulong valueVn2,
                           [Values] bool op)
        {
            uint opcode = 0xee000a10; // invalid
            opcode |= (vn & 1) << 7;
            opcode |= (vn & 0x1e) << 15;
            opcode |= (rt & 0xf) << 12;

            if (op) opcode |= 1 << 20;

            SingleOpcode(opcode, r0: valueRn, r1: valueRn, r2: valueRn, r3: valueRn, v0: new V128(valueVn1, valueVn2)); //correct

            CompareAgainstUnicorn();
        }
#endif
    }
}
