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

        [Test, Combinatorial, Description("VTRN.<size> <Vd>, <Vm>")]
        public void Vtrn([Values(0u, 1u, 2u, 3u)] uint vm,
                   [Values(0u, 1u, 2u, 3u)] uint vd,
                   [Values(0u, 1u, 2u)] uint size,
                   [Values] bool q)
        {
            uint opcode = 0xf3b20080;
            if (vm == vd) return; //undefined

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1; vm <<= 1;
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3); //correct

            CompareAgainstUnicorn();
        }

        [Test, Combinatorial, Description("VZIP.<size> <Vd>, <Vm>")]
        public void Vzip([Values(0u, 1u, 2u, 3u)] uint vm,
           [Values(0u, 1u, 2u, 3u)] uint vd,
           [Values(0u, 1u, 2u)] uint size,
           [Values] bool q)
        {
            uint opcode = 0xf3b20180;
            if (vm == vd || (size == 2 && !q)) return; //undefined

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1; vm <<= 1;
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3); //correct

            CompareAgainstUnicorn();
        }

        [Test, Combinatorial, Description("VUZP.<size> <Vd>, <Vm>")]
        public void Vuzp([Values(0u, 1u, 2u, 3u)] uint vm,
           [Values(0u, 1u, 2u, 3u)] uint vd,
           [Values(0u, 1u, 2u)] uint size,
           [Values] bool q)
        {
            uint opcode = 0xf3b20100;
            if (vm == vd || (size == 2 && !q)) return; //undefined

            if (q)
            {
                opcode |= 1 << 6;
                vd <<= 1; vm <<= 1;
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;
            opcode |= (size & 0x3) << 18;

            V128 v0 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v1 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v2 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3); //correct

            CompareAgainstUnicorn();
        }

        [Test, Combinatorial, Description("VTBL.8 <Dd>, {list}, <Dm>")]
        public void Vtbl([Range(0u, 6u)] uint vm, //indices, include potentially invalid
            [Range(4u, 12u)] uint vn, //selection
           [Values(0u, 1u)] uint vd, //destinations
           [Range(0u, 3u)] uint length,
           [Values] bool x)
        {
            uint opcode = 0xf3b00800;
            if (vn + length > 31) return; //undefined

            if (x)
            {
                opcode |= 1 << 6;
            }
            opcode |= (vm & 0x10) << 1;
            opcode |= (vm & 0xf);
            opcode |= (vd & 0x10) << 18;
            opcode |= (vd & 0xf) << 12;

            opcode |= (vn & 0x10) << 3;
            opcode |= (vn & 0xf) << 16;
            opcode |= (length & 0x3) << 8;

            var rnd = TestContext.CurrentContext.Random;
            V128 v2 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v3 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v4 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());
            V128 v5 = new V128(TestContext.CurrentContext.Random.NextULong(), TestContext.CurrentContext.Random.NextULong());

            byte maxIndex = (byte)(length * 8 - 1);
            byte[] b0 = new byte[16];
            byte[] b1 = new byte[16];
            for (int i=0; i<16; i++)
            {
                b0[i] = rnd.NextByte(maxIndex);
                b1[i] = rnd.NextByte(maxIndex);
            }

            V128 v0 = new V128(b0);
            V128 v1 = new V128(b1);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2, v3: v3, v4: v4, v5: v5); //correct

            CompareAgainstUnicorn();
        }
#endif
    }
}
