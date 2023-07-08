#define Alu32

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("Alu32")]
    public sealed class CpuTestAlu32 : CpuTest32
    {
#if Alu32

        #region "ValueSource (Opcodes)"
        private static uint[] SuHAddSub8()
        {
            return new[]
            {
                0xe6100f90u, // SADD8  R0, R0, R0
                0xe6100ff0u, // SSUB8  R0, R0, R0
                0xe6300f90u, // SHADD8 R0, R0, R0
                0xe6300ff0u, // SHSUB8 R0, R0, R0
                0xe6500f90u, // UADD8  R0, R0, R0
                0xe6500ff0u, // USUB8  R0, R0, R0
                0xe6700f90u, // UHADD8 R0, R0, R0
                0xe6700ff0u, // UHSUB8 R0, R0, R0
            };
        }

        private static uint[] SsatUsat()
        {
            return new[]
            {
                0xe6a00010u, // SSAT R0, #1, R0, LSL #0
                0xe6a00050u, // SSAT R0, #1, R0, ASR #32
                0xe6e00010u, // USAT R0, #0, R0, LSL #0
                0xe6e00050u, // USAT R0, #0, R0, ASR #32
            };
        }

        private static uint[] Ssat16Usat16()
        {
            return new[]
            {
                0xe6a00f30u, // SSAT16 R0, #1, R0
                0xe6e00f30u, // USAT16 R0, #0, R0
            };
        }

        private static uint[] LsrLslAsrRor()
        {
            return new[]
            {
                0xe1b00030u, // LSRS R0, R0, R0
                0xe1b00010u, // LSLS R0, R0, R0
                0xe1b00050u, // ASRS R0, R0, R0
                0xe1b00070u, // RORS R0, R0, R0
            };
        }
        #endregion

        private const int RndCnt = 2;

        private static readonly uint[] _testData_rd = {0u, 0xdu};
        private static readonly uint[] _testData_rm = {1u, 0xdu};
        private static readonly uint[] _testData_wn =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint> TestData_32bit = new(_testData_rd, _testData_rm, _testData_wn);

        [Theory(DisplayName = "RBIT <Rd>, <Rn>")]
        [MemberData(nameof(TestData_32bit))]
        public void Rbit_32bit(uint rd, uint rm, uint wn)
        {
            uint opcode = 0xe6ff0f30u; // RBIT R0, R0
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, int> TestData_Lsr = new(LsrLslAsrRor(), _testData_wn, RangeUtils.RangeData(0, 31, 1));

        [Theory]
        [MemberData(nameof(TestData_Lsr))]
        public void Lsr_Lsl_Asr_Ror(uint opcode, uint shiftValue, int shiftAmount)
        {
            uint rd = 0;
            uint rm = 1;
            uint rs = 2;
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rs & 15) << 8);

            SingleOpcode(opcode, r1: shiftValue, r2: (uint)shiftAmount);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Sh_rm =
        {
            1u,
        };

        private static readonly uint[] _testData_Sh_rn =
        {
            2u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Sh = new(_testData_rd, _testData_Sh_rm, _testData_Sh_rn, Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt));

        [Theory]
        [MemberData(nameof(TestData_Sh))]
        public void Shadd8(uint rd, uint rm, uint rn, uint w0, uint w1, uint w2)
        {
            uint opcode = 0xE6300F90u; // SHADD8 R0, R0, R0

            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        [Theory]
        [MemberData(nameof(TestData_Sh))]
        public void Shsub8(uint rd, uint rm, uint rn, uint w0, uint w1, uint w2)
        {
            uint opcode = 0xE6300FF0u; // SHSUB8 R0, R0, R0

            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_sat =
        {
            0u, 7u, 8u, 0xfu, 0x10u, 0x1fu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Ssat = new(SsatUsat(), _testData_rd, _testData_rm, _testData_sat, _testData_sat, _testData_wn);

        [Theory]
        [MemberData(nameof(TestData_Ssat))]
        public void Ssat_Usat(uint opcode, uint rd, uint rn, uint sat, uint shift, uint wn)
        {
            opcode |= ((rn & 15) << 0) | ((shift & 31) << 7) | ((rd & 15) << 12) | ((sat & 31) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_sat16 =
        {
            0u, 7u, 8u, 0xfu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Ssat16 = new(Ssat16Usat16(), _testData_rd, _testData_rm, _testData_sat16, _testData_wn);

        [Theory]
        [MemberData(nameof(TestData_Ssat16))]
        public void Ssat16_Usat16(uint opcode, uint rd, uint rn, uint sat, uint wn)
        {
            opcode |= ((rn & 15) << 0) | ((rd & 15) << 12) | ((sat & 15) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint> TestData_Su = new(SuHAddSub8(), _testData_rd, _testData_Sh_rm, _testData_Sh_rn, Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt));

        [Theory]
        [MemberData(nameof(TestData_Su))]
        public void SU_H_AddSub_8(uint opcode, uint rd, uint rm, uint rn, uint w0, uint w1, uint w2)
        {
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: w0, r1: w1, r2: w2, sp: sp);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Uadd_rd=
        {
            0u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Uadd = new(_testData_Uadd_rd, _testData_Sh_rm, _testData_Sh_rn, Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt));

        [Theory]
        [MemberData(nameof(TestData_Uadd))]
        public void Uadd8_Sel(uint rd, uint rm, uint rn, uint w0, uint w1, uint w2)
        {
            uint opUadd8 = 0xE6500F90; // UADD8 R0, R0, R0
            uint opSel = 0xE6800FB0; // SEL R0, R0, R0

            opUadd8 |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);
            opSel |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);

            SetContext(r0: w0, r1: w1, r2: w2);
            Opcode(opUadd8);
            Opcode(opSel);
            Opcode(0xE12FFF1E); // BX LR
            ExecuteOpcodes();

            CompareAgainstUnicorn();
        }
#endif
    }
}
