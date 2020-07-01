#define SimdLogical32

using ARMeilleure.State;
using NUnit.Framework;
using System;

namespace Ryujinx.Tests.Cpu
{
    [Category("SimdLogical32")]
    public sealed class CpuTestSimdLogical32 : CpuTest32
    {
#if SimdLogical32

#region "ValueSource (Opcodes)"
        private static uint[] _Vbic_Vbif_Vbit_Vbsl_Vand_Vorr_Veor_()
        {
            return new uint[]
            {
                0xf2100110u, // VBIC D0, D0, D0
                0xf3300110u, // VBIF D0, D0, D0
                0xf3200110u, // VBIT D0, D0, D0
                0xf3100110u, // VBSL D0, D0, D0
                0xf2000110u, // VAND D0, D0, D0
                0xf2200110u, // VORR D0, D0, D0
                0xf3000110u  // VEOR D0, D0, D0
            };
        }

        private static uint[] _Vbic_Vorr_()
        {
            return new uint[]
            {
                0xf2800130u, // VBIC.I32 D0, #0
                0xf2800110u  // VORR.I32 D0, #0
            };
        }
 #endregion

        private const int RndCnt = 2;

        [Test, Pairwise]
        public void Vbic_Vbif_Vbit_Vbsl_Vand_Vorr_Veor([ValueSource("_Vbic_Vbif_Vbit_Vbsl_Vand_Vorr_Veor_")] uint opcode,
                                                       [Range(0u, 4u)] uint rd,
                                                       [Range(0u, 4u)] uint rn,
                                                       [Range(0u, 4u)] uint rm,
                                                       [Random(RndCnt)] ulong z,
                                                       [Random(RndCnt)] ulong a,
                                                       [Random(RndCnt)] ulong b,
                                                       [Values] bool q)
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

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise]
        public void Vbic_Vorr_II([ValueSource("_Vbic_Vorr_")] uint opcode,
                                 [Range(0u, 4u)] uint rd,
                                 [Random(RndCnt)] ulong z,
                                 [Random(RndCnt)] byte imm,
                                 [Values(0u, 1u, 2u, 3u)] uint cMode,
                                 [Values] bool q)
        {
            if (q)
            {
                opcode |= 1 << 6;
                rd <<= 1;
            }

            opcode |= (uint)(imm & 0xf) << 0;
            opcode |= (uint)(imm & 0x70) << 12;
            opcode |= (uint)(imm & 0x80) << 17;
            opcode |= (cMode & 0x3) << 9;
            opcode |= ((rd & 0xf) << 12) | ((rd & 0x10) << 18);

            V128 v0 = MakeVectorE0E1(z, z);

            SingleOpcode(opcode, v0: v0);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("VTST.<dt> <Vd>, <Vn>, <Vm>")]
        public void Vtst([Values(0u)] uint rd,
                         [Values(1u, 0u)] uint rn,
                         [Values(2u, 0u)] uint rm,
                         [Values(0u, 1u, 2u)] uint size,
                         [Random(RndCnt)] ulong z,
                         [Random(RndCnt), Values(0xffff0000ffff0000UL, 0xff00ff0000ff00ffUL)] ulong a,
                         [Random(RndCnt), Values(0xaaaa000000000000UL, 0x00ff00ff00ff00ffUL)] ulong b,
                         [Values] bool q)
        {
            uint opcode = 0xf2000810u; // VTST.8 D0, D0, D0

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

            opcode |= size << 20;

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(a, z);
            V128 v2 = MakeVectorE0E1(b, z);

            SingleOpcode(opcode, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }
#endif
    }
}
