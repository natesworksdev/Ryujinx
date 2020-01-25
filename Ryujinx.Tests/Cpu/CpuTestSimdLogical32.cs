#define SimdLogical32
using ARMeilleure.State;
using NUnit.Framework;
using System;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdLogical32")]
    class CpuTestSimdLogical32 : CpuTest32
    {
#if SimdLogical32

        private const int RndCnt = 5;

        private uint GenerateVectorOpcode(uint opcode, uint rd, uint rn, uint rm, bool q)
        {
            if (q)
            {
                opcode |= 1 << 6;
                rm <<= 1;
                rn <<= 1;
                rd <<= 1;
            }

            opcode |= ((rm & 0xf) << 0) | ((rm & 0x10) << 1);
            opcode |= ((rd & 0xf) << 12) | ((rd & 0x10) << 18);
            opcode |= ((rn & 0xf) << 16) | ((rn & 0x10) << 3);

            return opcode;
        }

        [Test, Pairwise, Description("VBIF {<Vd>}, <Vm>, <Vn>")]
        public void Vbif([Range(0u, 4u)] uint rd,
                         [Range(0u, 4u)] uint rn,
                         [Range(0u, 4u)] uint rm,
                         [Random(RndCnt)] ulong z,
                         [Random(RndCnt)] ulong a,
                         [Random(RndCnt)] ulong b,
                         [Values] bool q)
        {
            uint opcode = GenerateVectorOpcode(0xf3300110u, rd, rn, rm, q); // VBIF D0, D0, D0

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VBIT {<Vd>}, <Vm>, <Vn>")]
        public void Vbit([Range(0u, 4u)] uint rd,
                         [Range(0u, 4u)] uint rn,
                         [Range(0u, 4u)] uint rm,
                         [Random(RndCnt)] ulong z,
                         [Random(RndCnt)] ulong a,
                         [Random(RndCnt)] ulong b,
                         [Values] bool q)
        {
            uint opcode = GenerateVectorOpcode(0xf3200110u, rd, rn, rm, q); // VBIT D0, D0, D0

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VBSL {<Vd>}, <Vm>, <Vn>")]
        public void Vbsl([Range(0u, 4u)] uint rd,
                         [Range(0u, 4u)] uint rn,
                         [Range(0u, 4u)] uint rm,
                         [Random(RndCnt)] ulong z,
                         [Random(RndCnt)] ulong a,
                         [Random(RndCnt)] ulong b,
                         [Values] bool q)
        {
            uint opcode = GenerateVectorOpcode(0xf3100110u, rd, rn, rm, q); // VBSL D0, D0, D0

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VAND {<Vd>}, <Vm>, <Vn>")]
        public void Vand([Range(0u, 4u)] uint rd,
                         [Range(0u, 4u)] uint rn,
                         [Range(0u, 4u)] uint rm,
                         [Random(RndCnt)] ulong z,
                         [Random(RndCnt)] ulong a,
                         [Random(RndCnt)] ulong b,
                         [Values] bool q)
        {
            uint opcode = GenerateVectorOpcode(0xf2000110u, rd, rn, rm, q); // VAND D0, D0, D0

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }
#endif
    }
}
