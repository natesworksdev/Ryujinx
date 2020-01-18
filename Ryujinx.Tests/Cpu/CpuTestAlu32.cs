#define Alu32
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Tests.Cpu
{
#if Alu32
    [Category("Alu32")]
    class CpuTestAlu32 : CpuTest32
    {
        private const int RndCnt = 2;

        [Test, Pairwise, Description("RBIT <Rd>, <Rn>")]
        public void Rbit_32bit([Values(0u, 0xdu)] uint rd,
                               [Values(1u, 0xdu)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn)
        {
            uint opcode = 0xe6ff0f30; // RBIT R0, R0
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("LSRS {<Rd>,} <Rm>, <Rs>")]
        public void Lsr([Values(0x00000000u, 0x7FFFFFFFu,
                                0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint shiftValue,
                       [Range(0, 31)] [Values(32, 256, 768, -1, -23)] int shiftAmount)
        {
            uint opcode = 0xe1b00030; // LSRS R0, R0, R0
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("LSLS {<Rd>,} <Rm>, <Rs>")]
        public void Lsl([Values(0x00000000u, 0x7FFFFFFFu,
                                0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint shiftValue,
                        [Range(0, 31)] [Values(32, 256, 768, -1, -23)] int shiftAmount)
        {
            uint opcode = 0xe1b00010; // LSLS R0, R0, R0
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("ASRS {<Rd>,} <Rm>, <Rs>")]
        public void Asr([Values(0x00000000u, 0x7FFFFFFFu,
                                0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint shiftValue,
                        [Range(0, 31)] [Values(32, 256, 768, -1, -23)] int shiftAmount)
        {
            uint opcode = 0xe1b00050; // ASRS R0, R0, R0
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("RORS {<Rd>,} <Rm>, <Rs>")]
        public void Ror([Values(0x00000000u, 0x7FFFFFFFu,
                                0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint shiftValue,
                        [Range(0, 31)] [Values(32, 256, 768, -1, -23)] int shiftAmount)
        {
            uint opcode = 0xe1b00070; // RORS R0, R0, R0
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }
    }
#endif 
}
